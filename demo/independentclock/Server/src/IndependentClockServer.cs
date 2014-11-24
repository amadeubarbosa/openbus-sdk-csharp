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
using tecgraf.openbus.exceptions;
using tecgraf.openbus.security;

namespace demo {
  /// <summary>
  /// Servidor da demo Independent Clock.
  /// </summary>
  internal static class IndependentClockServer {
    private static Connection _conn;
    internal static ServiceOffer Offer;
    private static int _interval;
    private static string _host;
    private static ushort _port;
    private static string _entity;
    private static PrivateKey _privateKey;
    private static ComponentContext _component;
    private static Registerer _registerer;

    private static void Main(String[] args) {
      // Obtém dados através dos argumentos
      _host = args[0];
      _port = Convert.ToUInt16(args[1]);
      _entity = args[2];
      _privateKey = Crypto.ReadKeyFile(args[3]);
      _interval = Convert.ToInt32(args.Length > 4 ? args[4] : "1");

      // Registra handler para o caso do processo ser finalizado
      AppDomain.CurrentDomain.ProcessExit += CurrentDomainProcessExit;

      // Cria o componente que conterá as facetas do servidor
      _component =
        new DefaultComponentContext(new ComponentId("independentclock", 1, 0, 0,
                                                    ".net"));

      // Cria a faceta Clock para o componente
      ClockImpl clock = new ClockImpl();
      _component.AddFacet("Clock", Repository.GetRepositoryID(typeof (Clock)),
                          clock);

      // Inicia thread que tenta conectar ao barramento
      ThreadStart ts = ConnectToOpenBus;
      Thread t = new Thread(ts) {IsBackground = true};
      t.Start();

      // Realiza trabalho independente do OpenBus
      while (true) {
        clock.getTimeInTicks();
        //        Console.WriteLine(String.Format("Hora atual: {0:HH:mm:ss}", DateTime.Now));
        Thread.Sleep(_interval);
      }
    }

    private static void ConnectToOpenBus() {
      // Define propriedades para a oferta de serviço a ser registrada no barramento
      IComponent ic = _component.GetIComponent();
      ServiceProperty[] properties = {
                                       new ServiceProperty("offer.domain",
                                                           "Demo Independent Clock")
                                     };

      // Cria conexão e a define como conexão padrão tanto para entrada como saída.
      OpenBusContext context = ORBInitializer.Context;
      _conn = context.CreateConnection(_host, _port);
      context.SetDefaultConnection(_conn);

      // Cria registrador e adiciona a callback de login inválido
      _registerer = new Registerer(ic, properties, _interval);
      _conn.OnInvalidLogin = InvalidLogin;

      // Faz o login e registra no barramento
      try {
        _conn.OnInvalidLogin(_conn, new LoginInfo());
      }
      catch (Exception e) {
        Console.WriteLine(e);
      }
      finally {
        if (!_conn.Login.HasValue || Offer == null) {
          Exit(1);
        }
      }
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

    private static void InvalidLogin(Connection conn, LoginInfo login) {
      bool succeeded = false;
      while (!succeeded) {
        try {
          // Faz o login
          conn.LoginByCertificate(_entity, _privateKey);
          succeeded = true;
        }
        // Login
        catch (AlreadyLoggedInException) {
          // Ignora o erro e retorna, pois já está reautenticado e portanto já há uma thread tentando registrar
          return;
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
        if (succeeded) {
          // Inicia o processo de re-registro da oferta
          _registerer.Activate();
        }
        else {
          Thread.Sleep(_interval);
        }
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

  internal class Registerer {
    private bool _active;
    // lock propositalmente não é um ReaderWriterLockSlim pois não queremos otimizar as leituras
    private readonly object _lock;
    private readonly IComponent _ic;
    private readonly ServiceProperty[] _props;
    private readonly int _waitTime;

    internal Registerer(IComponent ic, ServiceProperty[] props, int waitTime) {
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
            IndependentClockServer.Offer =
              ORBInitializer.Context.OfferRegistry.registerService(_ic, _props);
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