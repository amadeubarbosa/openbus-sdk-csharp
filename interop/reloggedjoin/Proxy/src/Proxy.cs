using System;
using System.Threading;
using Ch.Elca.Iiop.Idl;
using Org.BouncyCastle.Crypto;
using Scs.Core;
using scs.core;
using tecgraf.openbus.core.v2_0.services.access_control;
using tecgraf.openbus.core.v2_0.services.offer_registry;
using tecgraf.openbus.exceptions;
using tecgraf.openbus.interop.relloggedjoin.Properties;
using tecgraf.openbus.interop.simple;
using tecgraf.openbus.security;

namespace tecgraf.openbus.interop.relloggedjoin.src {
  internal static class Proxy {
    private const string Entity = "interop_reloggedjoin_csharp_proxy";
    private static AsymmetricCipherKeyPair _privateKey;
    private static IComponent _ic;
    private static ServiceProperty[] _properties;

    private static void Main() {
      AppDomain.CurrentDomain.ProcessExit += CurrentDomainProcessExit;
      string hostName = DemoConfig.Default.busHostName;
      ushort hostPort = DemoConfig.Default.busHostPort;
      _privateKey = Crypto.ReadKeyFile(DemoConfig.Default.privateKey);

      ConnectionProperties props = new ConnectionPropertiesImpl();
      props.AccessKey = _privateKey;
      OpenBusContext context = ORBInitializer.Context;
      Connection conn = context.CreateConnection(hostName, hostPort, props);
      context.SetDefaultConnection(conn);

      conn.LoginByCertificate(Entity, _privateKey);

      ComponentContext component =
        new DefaultComponentContext(new ComponentId("Hello", 1, 0, 0, ".net"));
      HelloProxyImpl hello = new HelloProxyImpl(Entity, _privateKey);
      component.AddFacet("Hello",
                         Repository.GetRepositoryID(typeof (Hello)),
                         hello);

      _ic = component.GetIComponent();
      _properties = new[] {
        new ServiceProperty("offer.domain",
                            "Interoperability Tests"),
        new ServiceProperty("reloggedjoin.role",
                            "proxy")
      };
      context.OfferRegistry.registerService(_ic, _properties);
      conn.OnInvalidLogin = InvalidLogin;

      Console.WriteLine("Hello Proxy no ar.");

      Thread.Sleep(Timeout.Infinite);
    }

    private static void InvalidLogin(Connection conn, LoginInfo login) {
      try {
        Console.WriteLine(
          "Callback de InvalidLogin foi chamada, tentando logar novamente no barramento.");
        conn.LoginByCertificate(Entity, _privateKey);
        ORBInitializer.Context.OfferRegistry.registerService(_ic, _properties);
      }
      catch (AlreadyLoggedInException) {
        // outra thread reconectou
      }
      catch (Exception e) {
        Console.WriteLine(e);
      }
    }

    private static void CurrentDomainProcessExit(object sender, EventArgs e) {
      ORBInitializer.Context.GetCurrentConnection().Logout();
    }
  }
}