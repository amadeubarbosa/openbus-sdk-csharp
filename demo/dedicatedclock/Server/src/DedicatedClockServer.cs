using System;
using System.Threading;
using Ch.Elca.Iiop.Idl;
using Scs.Core;
using demo.Properties;
using omg.org.CORBA;
using scs.core;
using tecgraf.openbus;
using tecgraf.openbus.core.v2_0.services;
using tecgraf.openbus.core.v2_0.services.access_control;
using tecgraf.openbus.core.v2_0.services.offer_registry;
using tecgraf.openbus.security;

namespace demo {
  /// <summary>
  /// Servidor da demo Dedicated Clock.
  /// </summary>
  internal static class DedicatedClockServer {
    private static Connection _conn;
    internal static ServiceOffer Offer;

    private static void Main(String[] args) {
      // Registra handler para o caso do processo ser finalizado
      AppDomain.CurrentDomain.ProcessExit += CurrentDomainProcessExit;

      // Obtém dados através dos argumentos
      string host = args[0];
      ushort port = Convert.ToUInt16(args[1]);
      string entity = args[2];
      PrivateKey privateKey = Crypto.ReadKeyFile(args[3]);
      int interval = Convert.ToInt32(args.Length > 4 ? args[4] : "1");

      // Cria o componente que conterá as facetas do servidor
      ComponentContext component =
        new DefaultComponentContext(new ComponentId("dedicatedclock", 1, 0, 0, ".net"));

      // Cria a faceta Clock para o componente
      component.AddFacet("Clock", Repository.GetRepositoryID(typeof(Clock)),
                         new ClockImpl());

      // Define propriedades para a oferta de serviço a ser registrada no barramento
      IComponent ic = component.GetIComponent();
      ServiceProperty[] properties = new[] {
                                             new ServiceProperty("offer.domain",
                                                                 "Demo Dedicated Clock")
                                           };

      // Cria conexão e a define como conexão padrão tanto para entrada como saída.
      OpenBusContext context = ORBInitializer.Context;
      _conn = context.CreateConnection(host, port, null);
      context.SetDefaultConnection(_conn);

      // Cria registrador e adiciona a callback de login inválido
      Registerer registerer = new Registerer(ic, properties, interval);
      _conn.OnInvalidLogin = new DedicatedClockInvalidLoginCallback(entity, privateKey, registerer);

      // Faz o login e registra no barramento
      try {
        _conn.OnInvalidLogin.InvalidLogin(_conn, new LoginInfo());
      }
      catch (Exception e) {
        Console.WriteLine(e);
      }
      finally {
        if (!_conn.Login.HasValue || Offer == null) {
          Exit(1);
        }
      }

      // Mantém a thread ativa para aguardar requisições
      Console.WriteLine(Resources.ServerOK);
      Thread.Sleep(Timeout.Infinite);
    }

    private static void RemoveOfferAndLogout() {
      if (Offer != null) {
        try {
          Console.WriteLine(Resources.RemovingOffer);
          Offer.remove();
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
        catch (NO_PERMISSION e) {
          if (e.Minor == NoLoginCode.ConstVal) {
            Console.WriteLine(Resources.NoLoginCodeErrorMsg);
          }
          else {
            throw;
          }
        }
      }
      if (_conn.Login.HasValue) {
        try {
          _conn.Logout();
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
      }
    }

    private static void CurrentDomainProcessExit(object sender, EventArgs e) {
      RemoveOfferAndLogout();
    }

    private static void Exit(int code) {
      RemoveOfferAndLogout();
      Console.WriteLine(Resources.PressAnyKeyToExit);
      Console.ReadLine();
      Environment.Exit(code);
    }
  }

  internal class Registerer {
    private bool _active;
    // lock propositalmente não é um ReaderWriterLockSlim pois não queremos otimizar as leituras
    private readonly object _lock;
    private readonly IComponent _ic;
    private readonly ServiceProperty[] _props;
    private readonly int _waitTime;

    internal Registerer (IComponent ic, ServiceProperty[] props, int waitTime) {
      _ic = ic;
      _props = props;
      _waitTime = waitTime;
      _lock = new object();
      _active = false;
    }

    internal void Activate() {
      bool register = false;
      lock (_lock) {
        if (!_active) {
          register = _active = true;
        }
      }
      if (register) {
        bool succeeded = false;
        while (!succeeded) {
          try {
            // Registra a oferta no barramento
            DedicatedClockServer.Offer = ORBInitializer.Context.OfferRegistry.registerService(_ic, _props);
            lock (_lock) {
              // libera o registerer para novo uso
              _active = false;
            }
            succeeded = true;
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
          catch (NO_PERMISSION e) {
            if (e.Minor == NoLoginCode.ConstVal) {
              Console.WriteLine(Resources.NoLoginCodeErrorMsg);
            }
            else {
              throw;
            }
          }
          finally {
            if (!succeeded) {
              Thread.Sleep(_waitTime);
            }
          }
        }
      }
    }
  }
}