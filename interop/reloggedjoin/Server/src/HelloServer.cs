using System;
using System.IO;
using System.Threading;
using Ch.Elca.Iiop.Idl;
using Scs.Core;
using log4net.Config;
using scs.core;
using tecgraf.openbus.core.v2_1.services.access_control;
using tecgraf.openbus.core.v2_1.services.offer_registry;
using tecgraf.openbus.exceptions;
using tecgraf.openbus.interop.relloggedjoin.Properties;
using tecgraf.openbus.interop.simple;
using tecgraf.openbus.security;

namespace tecgraf.openbus.interop.relloggedjoin {
  /// <summary>
  ///   Servidor do teste de interoperabilidade reloggedjoin.
  /// </summary>
  internal static class HelloServer {
    private const string Entity = "interop_reloggedjoin_csharp_server";
    private static Connection _conn;
    private static PrivateKey _privateKey;
    private static IComponent _ic;
    private static ServiceProperty[] _properties;
    private static ServiceOffer _offer;

    private static void Main() {
      AppDomain.CurrentDomain.ProcessExit += CurrentDomainProcessExit;
      string hostName = DemoConfig.Default.busHostName;
      ushort hostPort = DemoConfig.Default.busHostPort;
      _privateKey = Crypto.ReadKeyFile(DemoConfig.Default.privateKey);

      FileInfo logFileInfo = new FileInfo(DemoConfig.Default.openbusLogFile);
      XmlConfigurator.ConfigureAndWatch(logFileInfo);

      ConnectionProperties props = new ConnectionPropertiesImpl();
      props.AccessKey = _privateKey;
      OpenBusContext context = ORBInitializer.Context;
      _conn = context.CreateConnection(hostName, hostPort, props);
      context.SetDefaultConnection(_conn);

      ComponentContext component =
        new DefaultComponentContext(new ComponentId("hello", 1, 0, 0, ".net"));
      component.AddFacet("Hello", Repository.GetRepositoryID(typeof (Hello)),
                         new HelloImpl());

      _conn.LoginByCertificate(Entity, _privateKey);

      _ic = component.GetIComponent();
      _properties = new[] {
        new ServiceProperty("offer.domain",
                            "Interoperability Tests"),
        new ServiceProperty("reloggedjoin.role",
                            "server")
      };
      _offer = context.OfferRegistry.registerService(_ic, _properties);
      _conn.OnInvalidLogin = InvalidLogin;

      Console.WriteLine("Servidor no ar.");
      Thread.Sleep(Timeout.Infinite);
    }

    private static void CurrentDomainProcessExit(object sender, EventArgs e) {
      if (_offer != null) {
        try {
          _offer.remove();
        }
        catch (Exception exc) {
          Console.WriteLine(
            "Erro ao remover a oferta antes de finalizar o processo: " + exc);
        }
      }
      _conn.Logout();
    }

    private static void InvalidLogin(Connection conn, LoginInfo login) {
      OpenBusContext context = ORBInitializer.Context;
      context.SetCurrentConnection(conn);
      try {
        Console.WriteLine(
          "Callback de InvalidLogin foi chamada, tentando logar novamente no barramento.");
        conn.LoginByCertificate(Entity, _privateKey);
        _offer = context.OfferRegistry.registerService(_ic, _properties);
      }
      catch (AlreadyLoggedInException) {
        // outra thread reconectou
      }
      catch (Exception e) {
        Console.WriteLine(e);
      }
    }
  }
}