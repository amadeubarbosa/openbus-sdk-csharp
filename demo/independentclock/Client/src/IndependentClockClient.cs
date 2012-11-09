using System;
using System.Text;
using System.Threading;
using Ch.Elca.Iiop.Idl;
using demo.Properties;
using omg.org.CORBA;
using tecgraf.openbus;
using tecgraf.openbus.core.v2_0.services;
using tecgraf.openbus.core.v2_0.services.access_control;
using tecgraf.openbus.core.v2_0.services.offer_registry;
using tecgraf.openbus.exceptions;

namespace demo {
  /// <summary>
  /// Cliente da demo Independent Clock.
  /// </summary>
  internal static class IndependentClockClient {
    private static Connection _conn;
    private static Finder _finder;
    private static int _interval;
    private static string _entity;
    private static byte[] _password;

    private static void Main(String[] args) {
      // Obtém dados através dos argumentos
      string host = args[0];
      ushort port = Convert.ToUInt16(args[1]);
      _entity = args[2];
      _password = new ASCIIEncoding().GetBytes(args[3]);
      _interval = Convert.ToInt32(args.Length > 4 ? args[4] : "1");

      // Cria o finder que será responsável por encontrar o servidor no barramento
      _finder = new Finder(_interval);

      // Inicia thread que imprime a hora
      ThreadStart ts = PrintTime;
      Thread t = new Thread(ts);
      t.Start();

      // Cria conexão e a define como conexão padrão tanto para entrada como saída.
      // O uso exclusivo da conexão padrão (sem uso de current e callback de despacho) só é recomendado para aplicações que criem apenas uma conexão e desejem utilizá-la em todos os casos. Para situações diferentes, consulte o manual do SDK OpenBus e/ou outras demos.
      OpenBusContext context = ORBInitializer.Context;
      _conn = context.CreateConnection(host, port, null);
      context.SetDefaultConnection(_conn);

      // Define a callback de login inválido e faz o login
      _conn.OnInvalidLogin = InvalidLogin;
      _conn.OnInvalidLogin(_conn, new LoginInfo());
    }

    private static void PrintTime() {
      while (true) {
        bool failed = true;
        Clock clock = _finder.Clock;
        if (clock != null) {
          try {
            // utiliza o serviço
            long ticks = clock.getTimeInTicks();
            Console.WriteLine(String.Format("Hora do servidor: {0:HH:mm:ss}",
                                            new DateTime(ticks)));
            failed = false;
          }
          catch (TRANSIENT) {
            Console.WriteLine(Resources.ServiceTransientErrorMsg);
          }
          catch (COMM_FAILURE) {
            Console.WriteLine(Resources.ServiceCommFailureErrorMsg);
          }
          catch (NO_PERMISSION e) {
            bool found = false;
            string message = String.Empty;
            switch (e.Minor) {
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
          if (failed) {
            // descarta o clock atual e busca um novo
            _finder.Clock = null;
          }
        }
        if (failed) {
          _finder.Activate();
          Console.WriteLine(String.Format("Hora local: {0:HH:mm:ss}",
                                          DateTime.Now));
        }
        Thread.Sleep(_interval);
      }
    }

    private static void InvalidLogin(Connection conn, LoginInfo login) {
      bool failed = true;
      do {
        try {
          // Faz o login
          conn.LoginByPassword(_entity, _password);
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
        catch (NO_PERMISSION e) {
          if (e.Minor == NoLoginCode.ConstVal) {
            Console.WriteLine(Resources.NoLoginCodeErrorMsg);
          }
          else {
            throw;
          }
        }
      } while (failed);
      _finder.Activate();
    }
  }

  internal class Finder {
    private Clock _clock;
    private readonly int _interval;
    private bool _active;
    // lock propositalmente não é um ReaderWriterLockSlim pois não queremos otimizar as leituras
    private readonly object _lock;
    private readonly ServiceProperty[] _properties;

    internal Finder(int interval) {
      _interval = interval;
      _clock = null;
      _active = false;
      _lock = new object();
      // propriedade gerada automaticamente
      ServiceProperty autoProp =
        new ServiceProperty("openbus.component.interface",
                            Repository.GetRepositoryID(typeof (Clock)));
      // propriedade definida pelo serviço independent clock
      ServiceProperty prop = new ServiceProperty("offer.domain",
                                                 "Demo Independent Clock");
      _properties = new[] {prop, autoProp};
    }

    internal Clock Clock {
      get {
        lock (_lock) {
          return _clock;
        }
      }
      set {
        lock (_lock) {
          _clock = value;
        }
      }
    }

    internal void Activate() {
      bool find = false;
      lock (_lock) {
        if (_clock == null && !_active) {
          find = _active = true;
        }
      }
      if (find) {
        // faz a busca em outra thread para não travar essa
        ThreadStart ts = Find;
        Thread finderThread = new Thread(ts) { IsBackground = true };
        finderThread.Start();
      }
    }

    private void Find() {
      try {
        // Faz busca utilizando propriedades geradas automaticamente e propriedades definidas pelo serviço específico
        ServiceOfferDesc[] offers = null;
        while (true) {
          try {
            offers =
              ORBInitializer.Context.OfferRegistry.findServices(_properties);
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
          catch (NO_PERMISSION e) {
            if (e.Minor == NoLoginCode.ConstVal) {
              Console.WriteLine(Resources.NoLoginCodeErrorMsg);
            }
            else {
              throw;
            }
          }
          // analiza as ofertas encontradas
          bool foundClock = false;
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
                    serviceOfferDesc.service_ref.getFacet(
                      Repository.GetRepositoryID(typeof(Clock)));
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
                  // achou o serviço
                  lock (_lock) {
                    _clock = clock;
                    _active = false;
                    foundClock = true;
                  }
                  break;
                }
                catch (TRANSIENT) {
                  Console.WriteLine(Resources.ServiceTransientErrorMsg);
                }
                catch (COMM_FAILURE) {
                  Console.WriteLine(Resources.ServiceCommFailureErrorMsg);
                }
                catch (NO_PERMISSION e) {
                  bool found = false;
                  string message = String.Empty;
                  switch (e.Minor) {
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
              if (foundClock) {
                break;
              }
              Console.WriteLine(Resources.OfferFunctionalNotFound);
            }
          }
          Thread.Sleep(_interval);
        }
      }
      catch (ThreadInterruptedException) {
        // não faz nada, thread principal abortou essa thread
      }
    }
  }
}