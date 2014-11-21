using System;
using System.Threading;
using Ch.Elca.Iiop.Idl;
using Scs.Core;
using scs.core;
using tecgraf.openbus.core.v2_1.services.access_control;
using tecgraf.openbus.core.v2_1.services.offer_registry;
using tecgraf.openbus.exceptions;
using tecgraf.openbus.interop.multiplexing.Properties;
using tecgraf.openbus.interop.simple;
using tecgraf.openbus.security;

namespace tecgraf.openbus.interop.multiplexing {
  internal static class Server {
    private const string Entity = "interop_multiplexing_csharp_server";
    private static PrivateKey _privateKey;
    private static IComponent _ic;

    private static readonly ServiceProperty[] ServiceProperties =
      {new ServiceProperty("offer.domain", "Interoperability Tests")};

    private static Connection _conn1AtBus1;
    private static Connection _connAtBus2;

    private static void Main() {
      string hostName = DemoConfig.Default.busHostName;
      ushort hostPort = DemoConfig.Default.busHostPort;
      ushort hostPort2 = DemoConfig.Default.bus2HostPort;
      _privateKey = Crypto.ReadKeyFile(DemoConfig.Default.privateKey);

      // setup and start the orb
      OpenBusContext context = ORBInitializer.Context;

      // connect to the bus
      ConnectionProperties props = new ConnectionPropertiesImpl();
      props.AccessKey = _privateKey;
      _conn1AtBus1 = context.CreateConnection(hostName, hostPort, props);
      Connection conn2AtBus1 = context.CreateConnection(hostName, hostPort,
                                                        props);
      Connection conn3AtBus1 = context.CreateConnection(hostName, hostPort,
                                                        props);
      _connAtBus2 = context.CreateConnection(hostName, hostPort2, props);

      // create service SCS component
      ComponentId id = new ComponentId("Hello", 1, 0, 0, ".net");
      ComponentContext component = new DefaultComponentContext(id);
      component.AddFacet("Hello", Repository.GetRepositoryID(typeof (Hello)),
                         new HelloImpl());
      _ic = component.GetIComponent();

      // login to the bus
      _conn1AtBus1.LoginByCertificate(Entity, _privateKey);
      conn2AtBus1.LoginByCertificate(Entity, _privateKey);
      conn3AtBus1.LoginByCertificate(Entity, _privateKey);
      _connAtBus2.LoginByCertificate(Entity, _privateKey);

      // set incoming connections
      context.OnCallDispatch = Dispatch;

      RegisterThreadStart start1 = new RegisterThreadStart(_conn1AtBus1,
                                                           component.
                                                             GetIComponent());
      Thread thread1 = new Thread(start1.Run);
      thread1.Start();
      _conn1AtBus1.OnInvalidLogin = InvalidLogin;

      RegisterThreadStart start2 = new RegisterThreadStart(conn2AtBus1,
                                                           component.
                                                             GetIComponent());
      Thread thread2 = new Thread(start2.Run);
      thread2.Start();
      conn2AtBus1.OnInvalidLogin = InvalidLogin;

      RegisterThreadStart start3 = new RegisterThreadStart(conn3AtBus1,
                                                           component.
                                                             GetIComponent());
      Thread thread3 = new Thread(start3.Run);
      thread3.Start();
      conn3AtBus1.OnInvalidLogin = InvalidLogin;

      context.SetCurrentConnection(_connAtBus2);
      context.OfferRegistry.registerService(_ic, ServiceProperties);
      _connAtBus2.OnInvalidLogin = InvalidLogin;

      Console.WriteLine("Servidor no ar.");

      Thread.Sleep(Timeout.Infinite);
    }

    private static Connection Dispatch(OpenBusContext context, string busid,
                               string loginId, string uri, string operation) {
      if (busid.Equals(_conn1AtBus1.BusId)) {
        return _conn1AtBus1;
      }
      return busid.Equals(_connAtBus2.BusId) ? _connAtBus2 : null;
    }

    private static void InvalidLogin(Connection conn, LoginInfo login) {
      try {
        Console.WriteLine("Callback de InvalidLogin da conexão " + Entity +
                          " foi chamada, tentando logar novamente no barramento.");
        conn.LoginByCertificate(Entity, _privateKey);
        ORBInitializer.Context.OfferRegistry.registerService(_ic, ServiceProperties);
      }
      catch (AlreadyLoggedInException) {
        // outra thread reconectou
      }
      catch (Exception e) {
        Console.WriteLine(e);
      }
    }

    private class RegisterThreadStart {
      private readonly Connection _conn;
      private readonly IComponent _component;

      public RegisterThreadStart(Connection conn, IComponent component) {
        _conn = conn;
        _component = component;
      }

      public void Run() {
        OpenBusContext context = ORBInitializer.Context;
        context.SetCurrentConnection(_conn);
        context.OfferRegistry.registerService(_component, ServiceProperties);
      }
    }
  }
}