using System;
using System.Reflection;
using System.Text;
using System.Threading;
using Ch.Elca.Iiop.Idl;
using demo.Properties;
using omg.org.CORBA;
using tecgraf.openbus;
using tecgraf.openbus.core.v2_1.services;
using tecgraf.openbus.core.v2_1.services.access_control;
using tecgraf.openbus.core.v2_1.services.offer_registry;
using tecgraf.openbus.exceptions;

namespace demo {
  /// <summary>
  /// Cliente da demo dedicated clock.
  /// </summary>
  internal static class DedicatedClockClient {
    private static string _entity;
    private static byte[] _password;
    private static string _domain;
    private static int _retries;
    private static int _interval;

    private static void Main(String[] args) {
      // Obtém dados através dos argumentos
      string host = args[0];
      ushort port = Convert.ToUInt16(args[1]);
      _domain = args[2];
      _entity = args[3];
      _password = new ASCIIEncoding().GetBytes(args.Length > 4 ? args[4] : _entity);
      _interval = Convert.ToInt32(args.Length > 5 ? args[5] : "1");
      _retries = Convert.ToInt32(args.Length > 6 ? args[6] : "10");

      // Cria conexão e a define como conexão padrão tanto para entrada como saída.
      // O uso exclusivo da conexão padrão (sem uso de current e callback de despacho) só é recomendado para aplicações que criem apenas uma conexão e desejem utilizá-la em todos os casos. Para situações diferentes, consulte o manual do SDK OpenBus e/ou outras demos.
      ORBInitializer.InitORB();
      OpenBusContext context = ORBInitializer.Context;
      Connection conn = context.ConnectByAddress(host, port);
      context.SetDefaultConnection(conn);

      // Define a callback de login inválido e faz o login
      conn.OnInvalidLogin = InvalidLogin;
      conn.OnInvalidLogin(conn, new LoginInfo());

      // Faz busca utilizando propriedades geradas automaticamente e propriedades definidas pelo serviço específico
      string clockIDLType = Repository.GetRepositoryID(typeof(Clock));
      // propriedade gerada automaticamente
      ServiceProperty autoProp =
        new ServiceProperty("openbus.component.interface", clockIDLType);
      // propriedade definida pelo serviço dedicated clock
      ServiceProperty prop = new ServiceProperty("offer.domain", "Demo Dedicated Clock");
      ServiceProperty[] properties = { prop, autoProp };
      ServiceOfferDesc[] offers = null;
      bool failed = true;
      do {
        try {
          offers = context.OfferRegistry.findServices(properties);
        }
        catch (ServiceFailure e) {
          Console.WriteLine(Resources.BusServiceFailureErrorMsg);
          Console.WriteLine(e);
        }
        catch (TRANSIENT) {
          Console.WriteLine(Resources.BusTransientErrorMsg);
        }
        catch (COMM_FAILURE) {
          Console.WriteLine(Resources.BusCommFailureErrorMsg);
        }
        catch (Exception e) {
          NO_PERMISSION npe = null;
          if (e is TargetInvocationException) {
            // caso seja uma exceção lançada pelo SDK, será uma NO_PERMISSION
            npe = e.InnerException as NO_PERMISSION;
          }
          if ((npe == null) && (!(e is NO_PERMISSION))) {
            // caso não seja uma NO_PERMISSION não é uma exceção esperada então deixamos passar.
            throw;
          }
          npe = npe ?? e as NO_PERMISSION;
          if (npe.Minor == NoLoginCode.ConstVal) {
            Console.WriteLine(Resources.NoLoginCodeErrorMsg);
          }
          else {
            throw;
          }
        }
        // analiza as ofertas encontradas
        if (offers != null) {
          if (offers.Length < 1) {
            Console.WriteLine(Resources.ServiceNotFound);
          }
          else {
            if (offers.Length > 1) {
              Console.WriteLine(Resources.ServiceFoundMoreThanExpected);
            }
            foreach (ServiceOfferDesc serviceOfferDesc in offers) {
              Console.WriteLine(Resources.ServiceFoundTesting);
              try {
                MarshalByRefObject clockObj =
                  serviceOfferDesc.service_ref.getFacet(clockIDLType);
                if (clockObj == null) {
                  Console.WriteLine(Resources.FacetNotFoundInOffer);
                  continue;
                }
                Clock clock = clockObj as Clock;
                if (clock == null) {
                  Console.WriteLine(Resources.FacetFoundWrongType);
                  continue;
                }
                Console.WriteLine(Resources.OfferFound);
                // utiliza o serviço
                long ticks = clock.getTimeInTicks();
                DateTime serverTime = new DateTime(ticks);
                Console.WriteLine("Hora do servidor: {0:HH:mm:ss}", serverTime);
                failed = false;
                break;
              }
              catch (TRANSIENT) {
                Console.WriteLine(Resources.ServiceTransientErrorMsg);
              }
              catch (COMM_FAILURE) {
                Console.WriteLine(Resources.ServiceCommFailureErrorMsg);
              }
              catch (Exception e) {
                NO_PERMISSION npe = null;
                if (e is TargetInvocationException) {
                  // caso seja uma exceção lançada pelo SDK, será uma NO_PERMISSION
                  npe = e.InnerException as NO_PERMISSION;
                }
                if ((npe == null) && (!(e is NO_PERMISSION))) {
                  // caso não seja uma NO_PERMISSION não é uma exceção esperada então deixamos passar.
                  throw;
                }
                npe = npe ?? e as NO_PERMISSION;
                bool found = false;
                string message = String.Empty;
                switch (npe.Minor) {
                  case NoLoginCode.ConstVal:
                    message = Resources.NoLoginCodeErrorMsg;
                    found = true;
                    break;
                  case UnknownBusCode.ConstVal:
                    message = Resources.UnknownBusCodeErrorMsg;
                    found = true;
                    break;
                  case UnverifiedLoginCode.ConstVal:
                    message = Resources.UnverifiedLoginCodeErrorMsg;
                    found = true;
                    break;
                  case InvalidRemoteCode.ConstVal:
                    message = Resources.InvalidRemoteCodeErrorMsg;
                    found = true;
                    break;
                }
                if (found) {
                  Console.WriteLine(message);
                }
                else {
                  throw;
                }
              }
            }
            if (failed) {
              Console.WriteLine(Resources.OfferFunctionalNotFound);
            }
          }
        }
      } while (failed && Retry());


      try {
        conn.Logout();
      }
      catch (ServiceFailure e) {
        Console.WriteLine(Resources.BusServiceFailureErrorMsg);
        Console.WriteLine(e);
      }
      catch (TRANSIENT) {
        Console.WriteLine(Resources.BusTransientErrorMsg);
      }
      catch (COMM_FAILURE) {
        Console.WriteLine(Resources.BusCommFailureErrorMsg);
      }
      if (!failed) {
        Console.WriteLine(Resources.ClientOK);
      }
      Console.ReadKey();
    }

    private static bool Retry() {
      if (_retries > 0) {
        _retries--;
        Thread.Sleep(_interval);
        return true;
      }
      return false;
    }

    private static void InvalidLogin(Connection conn, LoginInfo login) {
      bool failed = true;
      do {
        try {
          // Faz o login
          conn.LoginByPassword(_entity, _password, _domain);
          failed = false;
        }
        // Login
        catch (AlreadyLoggedInException) {
          // Ignora o erro
          failed = false;
        }
        catch (AccessDenied) {
          Console.WriteLine(Resources.ServerAccessDenied);
        }
        catch (MissingCertificate) {
          Console.WriteLine(Resources.MissingCertificateForEntity + _entity);
        }
        // Barramento
        catch (ServiceFailure e) {
          Console.WriteLine(Resources.BusServiceFailureErrorMsg);
          Console.WriteLine(e);
        }
        catch (TRANSIENT) {
          Console.WriteLine(Resources.BusTransientErrorMsg);
        }
        catch (COMM_FAILURE) {
          Console.WriteLine(Resources.BusCommFailureErrorMsg);
        }
        catch (Exception e) {
          NO_PERMISSION npe = null;
          if (e is TargetInvocationException) {
            // caso seja uma exceção lançada pelo SDK, será uma NO_PERMISSION
            npe = e.InnerException as NO_PERMISSION;
          }
          if ((npe == null) && (!(e is NO_PERMISSION))) {
            // caso não seja uma NO_PERMISSION não é uma exceção esperada então deixamos passar.
            throw;
          }
          npe = npe ?? e as NO_PERMISSION;
          if (npe.Minor == NoLoginCode.ConstVal) {
            Console.WriteLine(Resources.NoLoginCodeErrorMsg);
          }
          else {
            throw;
          }
        }
      } while (failed && Retry());
    }
  }
}