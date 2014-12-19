using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting;
using System.Threading;
using Ch.Elca.Iiop.Idl;
using Scs.Core;
using demo.Properties;
using omg.org.CORBA;
using scs.core;
using tecgraf.openbus;
using tecgraf.openbus.core.v2_1.services;
using tecgraf.openbus.core.v2_1.services.access_control;
using tecgraf.openbus.core.v2_1.services.offer_registry;
using tecgraf.openbus.security;

namespace demo {
  /// <summary>
  /// Servidor da demo multiplexing.
  /// </summary>
  internal static class MultiplexingServer {
    private static readonly IDictionary<Connection, ServiceOffer> Offers =
      new Dictionary<Connection, ServiceOffer>();

    private static readonly IDictionary<string, Connection> Connections =
      new Dictionary<string, Connection>();

    private static void Main(String[] args) {
      // Registra handler para o caso do processo ser finalizado
      AppDomain.CurrentDomain.ProcessExit += CurrentDomainProcessExit;

      // Obtém dados através dos argumentos
      string host = args[0];
      ushort port = Convert.ToUInt16(args[1]);
      string entity = args[2];
      PrivateKey privateKey = Crypto.ReadKeyFile(args[3]);

      // Associa uma callback de escolha de conexão de despacho
      ORBInitializer.InitORB();
      OpenBusContext context = ORBInitializer.Context;
      context.OnCallDispatch = Dispatch;

      // Cria 3 conexões com o mesmo barramento, uma para cada componente.
      for (int i = 0; i < 3; i++) {
        ComponentContext component =
          new DefaultComponentContext(new ComponentId("Timer", 1, 0, 0, ".net"));

        // Cria a faceta Timer para o componente
        TimerImpl timer = new TimerImpl();
        component.AddFacet("Timer", Repository.GetRepositoryID(typeof (Timer)),
                           timer);

        // Define propriedades para a oferta de serviço a ser registrada no barramento
        IComponent ic = component.GetIComponent();
        ServiceProperty[] properties = {
                                         new ServiceProperty(
                                           "offer.domain",
                                           "Demo Multiplexing")
                                       };

        // Cria a conexão e a define como conexão corrente
        Connection conn = context.ConnectByAddress(host, port);
        context.SetCurrentConnection(conn);

        // Associa a conexão à URI do servant para que a callback possa escolher
        Connections.Add(RemotingServices.GetObjectUri(timer), conn);

        bool failed = true;
        try {
          // Faz o login
          conn.LoginByCertificate(entity, privateKey);
          // Registra a oferta no barramento
          Offers.Add(conn, context.OfferRegistry.registerService(ic, properties));
          failed = false;
        }
          // Login
        catch (AccessDenied) {
          Console.WriteLine(Resources.ServerAccessDenied);
        }
        catch (MissingCertificate) {
          Console.WriteLine(Resources.MissingCertificateForEntity + entity);
        }
          // Registro
        catch (UnauthorizedFacets) {
          Console.WriteLine(Resources.UnauthorizedFacets);
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
        finally {
          if (failed) {
            Exit(1);
          }
        }
      }

      // Mantém a thread ativa para aguardar requisições
      Console.WriteLine(Resources.ServerOK);
      Thread.Sleep(Timeout.Infinite);
    }

    private static void RemoveOfferAndLogout() {
      if (Offers.Count > 0) {
        foreach (KeyValuePair<Connection, ServiceOffer> pair in Offers) {
          try {
            Console.WriteLine(Resources.RemovingOffer);
            pair.Value.remove();
            Console.WriteLine(Resources.RemovedOffer);
          }
          catch (UnauthorizedOperation) {
            Console.WriteLine(Resources.UnauthorizedRemoveOffer);
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
          Connection conn = pair.Key;
          if (conn.Login.HasValue) {
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
          }
        }
      }
    }

    private static Connection Dispatch(OpenBusContext context, string busid,
                                       string caller, string uri,
                                       string operation) {
      // tenta obter a conexão associada a esse servant. Se não conseguir, retorna qualquer uma disponível.
      lock (Connections) {
        return Connections.ContainsKey(uri)
                 ? Connections[uri]
                 : Connections.Values.FirstOrDefault();
      }
    }

    private static void CurrentDomainProcessExit(object sender, EventArgs e) {
      RemoveOfferAndLogout();
    }

    private static void Exit(int code) {
      RemoveOfferAndLogout();
      Console.WriteLine(Resources.PressAnyKeyToExit);
      Console.ReadKey();
      Environment.Exit(code);
    }
  }
}