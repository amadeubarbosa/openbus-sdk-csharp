﻿using System;
using System.Collections.Generic;
using System.Threading;
using Ch.Elca.Iiop.Idl;
using MultiplexingServer.Properties;
using Scs.Core;
using demoidl.hello;
using scs.core;
using tecgraf.openbus.core.v2_00.services.offer_registry;
using tecgraf.openbus.sdk;
using tecgraf.openbus.sdk.multiplexed;

namespace MultiplexingServer
{
  public class Server
  {
    private static readonly IList<Connection> Conns = new List<Connection>();

    public static void Main(string[] args)
    {
      try
      {
        AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
        string hostName = DemoConfig.Default.hostName;
        int hostPort = DemoConfig.Default.hostPort;
        int hostPort2 = DemoConfig.Default.hostPort2;

        // setup and start the orb
        MultiplexedOpenBus openbus = MultiplexedOpenBus.Instance as MultiplexedOpenBus;

        // connect to the bus
        Connection conn1AtBus1 = openbus.Connect(hostName, (short)hostPort);
        Connection conn2AtBus1 = openbus.Connect(hostName, (short)hostPort);
        Connection connAtBus2 = openbus.Connect(hostName, (short)hostPort2);

        Conns.Add(conn1AtBus1);
        Conns.Add(conn2AtBus1);
        Conns.Add(connAtBus2);

        // setup action on login termination
        conn1AtBus1.OnInvalidLoginCallback = new HelloInvalidLoginCallback("Conn1AtBus1");
        conn2AtBus1.OnInvalidLoginCallback = new HelloInvalidLoginCallback("Conn2AtBus1");
        connAtBus2.OnInvalidLoginCallback = new HelloInvalidLoginCallback("ConnAtBus2");

        // create service SCS component
        ComponentId id = new ComponentId("Hello", 1, 0, 0, ".net");
        ComponentContext context1 = new DefaultComponentContext(id);
        context1.AddFacet("Hello", Repository.GetRepositoryID(typeof(IHello)), new HelloImpl(Conns));

        // set incoming connection
        ConnectionMultiplexer multiplexer = openbus.Multiplexer;
        multiplexer.SetIncomingConnection(conn1AtBus1.BusId, conn1AtBus1);
        multiplexer.SetIncomingConnection(connAtBus2.BusId, connAtBus2);

        // login to the bus
        System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
        multiplexer.CurrentConnection = conn1AtBus1;
        conn1AtBus1.LoginByPassword("conn1", encoding.GetBytes("conn1"));
        multiplexer.CurrentConnection = conn2AtBus1;
        conn2AtBus1.LoginByPassword("conn2", encoding.GetBytes("conn2"));
        multiplexer.CurrentConnection = connAtBus2;
        connAtBus2.LoginByPassword("conn3", encoding.GetBytes("conn3"));
        multiplexer.CurrentConnection = null;

        RegisterThreadStart start1 = new RegisterThreadStart(conn1AtBus1, multiplexer, context1.GetIComponent());
        Thread thread1 = new Thread(start1.Run);
        thread1.Start();

        RegisterThreadStart start2 = new RegisterThreadStart(conn2AtBus1, multiplexer, context1.GetIComponent());
        Thread thread2 = new Thread(start2.Run);
        thread2.Start();

        multiplexer.CurrentConnection = connAtBus2;
        connAtBus2.OfferRegistry.registerService(context1.GetIComponent(), GetProps());
      }
      catch (Exception e)
      {
        Console.WriteLine(e.StackTrace);
      }
    }

    public static ServiceProperty[] GetProps() {
      ServiceProperty[] serviceProperties = new ServiceProperty[1];
      serviceProperties[0] = new ServiceProperty("offer.domain", "OpenBus Demos");
      return serviceProperties;
    }

    private class RegisterThreadStart {
      private readonly Connection _conn;
      private readonly ConnectionMultiplexer _multiplexer;
      private readonly IComponent _component;

      public RegisterThreadStart(Connection conn, ConnectionMultiplexer multiplexer, IComponent component) {
        _conn = conn;
        _multiplexer = multiplexer;
        _component = component;
      }

      public void Run() {
        _multiplexer.CurrentConnection = _conn;
        try {
          _conn.OfferRegistry.registerService(_component, GetProps());
        }
        catch (Exception e)
        {
          Console.WriteLine(e.StackTrace);
        }
      }
    }

    static void CurrentDomain_ProcessExit(object sender, EventArgs e) {
      foreach (Connection conn in Conns)
      {
        conn.Close();
      }
    }
  }
}