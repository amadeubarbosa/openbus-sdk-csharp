using System;
using System.Collections.Generic;
using System.Threading;
using Ch.Elca.Iiop.Idl;
using Scs.Core;
using scs.core;
using tecgraf.openbus.core.v2_00.services.offer_registry;
using tecgraf.openbus.interop.multiplexing.Properties;
using tecgraf.openbus.interop.simple;

namespace tecgraf.openbus.interop.multiplexing {
  public static class Server {
    private static readonly IList<Connection> Conns = new List<Connection>();

    public static void Main() {
      try {
        string hostName = DemoConfig.Default.hostName;
        short hostPort = DemoConfig.Default.hostPort;
        short hostPort2 = DemoConfig.Default.hostPort2;

        // setup and start the orb
        ConnectionManager manager = ORBInitializer.Manager;

        // connect to the bus
        Connection conn1AtBus1 = manager.CreateConnection(hostName, hostPort);
        Connection conn2AtBus1 = manager.CreateConnection(hostName, hostPort);
        Connection connAtBus2 = manager.CreateConnection(hostName, hostPort2);

        Conns.Add(conn1AtBus1);
        Conns.Add(conn2AtBus1);
        Conns.Add(connAtBus2);

        // setup action on login termination
        conn1AtBus1.OnInvalidLogin =
          new HelloInvalidLoginCallback("Conn1AtBus1", manager);
        conn2AtBus1.OnInvalidLogin =
          new HelloInvalidLoginCallback("Conn2AtBus1", manager);
        connAtBus2.OnInvalidLogin =
          new HelloInvalidLoginCallback("ConnAtBus2", manager);

        // create service SCS component
        ComponentId id = new ComponentId("Hello", 1, 0, 0, ".net");
        ComponentContext component = new DefaultComponentContext(id);
        component.AddFacet("Hello", Repository.GetRepositoryID(typeof (Hello)),
                           new HelloImpl(Conns));

        // login to the bus
        System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
        manager.Requester = conn1AtBus1;
        conn1AtBus1.LoginByPassword("conn1", encoding.GetBytes("conn1"));
        manager.Requester = conn2AtBus1;
        conn2AtBus1.LoginByPassword("conn2", encoding.GetBytes("conn2"));
        manager.Requester = connAtBus2;
        connAtBus2.LoginByPassword("conn3", encoding.GetBytes("conn3"));
        manager.Requester = null;

        // set incoming connection
        manager.SetDispatcher(conn1AtBus1);
        manager.SetDispatcher(connAtBus2);

        RegisterThreadStart start1 = new RegisterThreadStart(conn1AtBus1,
                                                             manager,
                                                             component.
                                                               GetIComponent());
        Thread thread1 = new Thread(start1.Run);
        thread1.Start();

        RegisterThreadStart start2 = new RegisterThreadStart(conn2AtBus1,
                                                             manager,
                                                             component.
                                                               GetIComponent());
        Thread thread2 = new Thread(start2.Run);
        thread2.Start();

        manager.Requester = connAtBus2;
        connAtBus2.Offers.registerService(component.GetIComponent(),
                                                 GetProps());

        Console.WriteLine("Servidor no ar.");

        Thread.Sleep(Timeout.Infinite);
      }
      catch (Exception e) {
        Console.WriteLine(e.Message);
        Console.WriteLine(e.StackTrace);
      }
    }

    private static ServiceProperty[] GetProps() {
      ServiceProperty[] serviceProperties = new ServiceProperty[1];
      serviceProperties[0] = new ServiceProperty("offer.domain", "Interoperability Tests");
      return serviceProperties;
    }

    private class RegisterThreadStart {
      private readonly Connection _conn;
      private readonly ConnectionManager _manager;
      private readonly IComponent _component;

      public RegisterThreadStart(Connection conn, ConnectionManager manager,
                                 IComponent component) {
        _conn = conn;
        _manager = manager;
        _component = component;
      }

      public void Run() {
        _manager.Requester = _conn;
        try {
          _conn.Offers.registerService(_component, GetProps());
        }
        catch (Exception e) {
          Console.WriteLine(e.Message);
          Console.WriteLine(e.StackTrace);
        }
      }
    }
  }
}