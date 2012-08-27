using System;
using System.Collections.Generic;
using System.Threading;
using Ch.Elca.Iiop.Idl;
using Scs.Core;
using scs.core;
using tecgraf.openbus.core.v2_0.services.offer_registry;
using tecgraf.openbus.interop.multiplexing.Properties;
using tecgraf.openbus.interop.simple;
using tecgraf.openbus.security;

namespace tecgraf.openbus.interop.multiplexing {
  internal static class Server {
    private static readonly IList<Connection> Conns = new List<Connection>();

    private static readonly ServiceProperty[] ServiceProperties =
      new[] {new ServiceProperty("offer.domain", "Interoperability Tests")};

    private static void Main() {
      string hostName = DemoConfig.Default.busHostName;
      ushort hostPort = DemoConfig.Default.busHostPort;
      ushort hostPort2 = DemoConfig.Default.bus2HostPort;
      const string entity = "interop_multiplexing_csharp_server";
      PrivateKey key = Crypto.ReadKeyFile(DemoConfig.Default.privateKey);

      // setup and start the orb
      OpenBusContext context = ORBInitializer.Context;

      // connect to the bus
      IDictionary<string, string> props = new Dictionary<string, string>();
      Connection conn1AtBus1 = context.CreateConnection(hostName, hostPort,
                                                        props);
      Connection conn2AtBus1 = context.CreateConnection(hostName, hostPort,
                                                        props);
      Connection conn3AtBus1 = context.CreateConnection(hostName, hostPort,
                                                        props);
      Connection connAtBus2 = context.CreateConnection(hostName, hostPort2,
                                                       props);

      Conns.Add(conn1AtBus1);
      Conns.Add(conn2AtBus1);
      Conns.Add(conn3AtBus1);
      Conns.Add(connAtBus2);

      // create service SCS component
      ComponentId id = new ComponentId("Hello", 1, 0, 0, ".net");
      ComponentContext component = new DefaultComponentContext(id);
      component.AddFacet("Hello", Repository.GetRepositoryID(typeof (Hello)),
                         new HelloImpl(Conns));
      IComponent ic = component.GetIComponent();

      // login to the bus
      conn1AtBus1.LoginByCertificate(entity, key);
      conn2AtBus1.LoginByCertificate(entity, key);
      conn3AtBus1.LoginByCertificate(entity, key);
      connAtBus2.LoginByCertificate(entity, key);

      // set incoming connections
      HelloCallDispatchCallback callDispatchCallback = new HelloCallDispatchCallback(conn1AtBus1, connAtBus2);
      context.OnCallDispatch = callDispatchCallback;

      RegisterThreadStart start1 = new RegisterThreadStart(conn1AtBus1,
                                                           component.
                                                             GetIComponent());
      Thread thread1 = new Thread(start1.Run);
      thread1.Start();
      conn1AtBus1.OnInvalidLogin = new HelloInvalidLoginCallback(entity, key,
                                                                 ic,
                                                                 ServiceProperties);

      RegisterThreadStart start2 = new RegisterThreadStart(conn2AtBus1,
                                                           component.
                                                             GetIComponent());
      Thread thread2 = new Thread(start2.Run);
      thread2.Start();
      conn2AtBus1.OnInvalidLogin = new HelloInvalidLoginCallback(entity, key,
                                                                 ic,
                                                                 ServiceProperties);

      RegisterThreadStart start3 = new RegisterThreadStart(conn3AtBus1,
                                                           component.
                                                             GetIComponent());
      Thread thread3 = new Thread(start3.Run);
      thread3.Start();
      conn3AtBus1.OnInvalidLogin = new HelloInvalidLoginCallback(entity, key,
                                                                 ic,
                                                                 ServiceProperties);

      context.SetCurrentConnection(connAtBus2);
      context.OfferRegistry.registerService(ic, ServiceProperties);
      connAtBus2.OnInvalidLogin = new HelloInvalidLoginCallback(entity, key,
                                                                ic,
                                                                ServiceProperties);

      Console.WriteLine("Servidor no ar.");

      Thread.Sleep(Timeout.Infinite);
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