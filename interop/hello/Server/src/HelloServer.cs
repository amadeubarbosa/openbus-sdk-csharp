using System;
using System.IO;
using System.Threading;
using Ch.Elca.Iiop.Idl;
using Scs.Core;
using log4net.Config;
using scs.core;
using tecgraf.openbus.core.v2_0.services.offer_registry;
using tecgraf.openbus.interop.simple.Properties;
using tecgraf.openbus.security;

namespace tecgraf.openbus.interop.simple {
  /// <summary>
  /// Servidor do teste de interoperabilidade hello.
  /// </summary>
  internal static class HelloServer {
    private static Connection _conn;
    internal static ServiceOffer Offer;

    private static void Main() {
      AppDomain.CurrentDomain.ProcessExit += CurrentDomainProcessExit;
      string hostName = DemoConfig.Default.busHostName;
      ushort hostPort = DemoConfig.Default.busHostPort;
      PrivateKey privateKey = Crypto.ReadKeyFile(DemoConfig.Default.privateKey);

      FileInfo logFileInfo = new FileInfo(DemoConfig.Default.openbusLogFile);
      XmlConfigurator.ConfigureAndWatch(logFileInfo);

      ConnectionProperties props = new ConnectionPropertiesImpl();
      props.AccessKey = privateKey;
      OpenBusContext context = ORBInitializer.Context;
      _conn = context.CreateConnection(hostName, hostPort, props);
      context.SetDefaultConnection(_conn);

      const string entityName = "interop_hello_csharp_server";

      ComponentContext component =
        new DefaultComponentContext(new ComponentId("hello", 1, 0, 0, ".net"));
      component.AddFacet("Hello", Repository.GetRepositoryID(typeof (Hello)),
                         new HelloImpl());

      _conn.LoginByCertificate(entityName, privateKey);

      IComponent ic = component.GetIComponent();
      ServiceProperty[] properties = new[] {
                                             new ServiceProperty("offer.domain",
                                                                 "Interoperability Tests")
                                           };
      Offer = context.OfferRegistry.registerService(ic, properties);
      _conn.OnInvalidLogin = new HelloInvalidLoginCallback(entityName,
                                                           privateKey, ic,
                                                           properties);

      Console.WriteLine("Servidor no ar.");
      Thread.Sleep(Timeout.Infinite);
    }

    private static void CurrentDomainProcessExit(object sender, EventArgs e) {
      if (Offer != null) {
        try {
          Offer.remove();
        }
        catch (Exception exc) {
          Console.WriteLine(
            "Erro ao remover a oferta antes de finalizar o processo: " + exc);
        }
      }
      _conn.Logout();
    }
  }
}