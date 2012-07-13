using System;
using System.IO;
using System.Threading;
using Ch.Elca.Iiop.Idl;
using Scs.Core;
using log4net.Config;
using scs.core;
using tecgraf.openbus.core.v2_0.services.offer_registry;
using tecgraf.openbus.interop.simple.Properties;

namespace tecgraf.openbus.interop.simple {
  /// <summary>
  /// Servidor do teste de interoperabilidade hello.
  /// </summary>
  internal static class HelloServer {
    private static Connection _conn;
    private static ServiceOffer _offer;

    private static void Main() {
      AppDomain.CurrentDomain.ProcessExit += CurrentDomainProcessExit;
      string hostName = DemoConfig.Default.hostName;
      short hostPort = DemoConfig.Default.hostPort;

      FileInfo logFileInfo = new FileInfo(DemoConfig.Default.logFile);
      XmlConfigurator.ConfigureAndWatch(logFileInfo);

      ConnectionManager manager = ORBInitializer.Manager;
      _conn = manager.CreateConnection(hostName, hostPort);
      manager.DefaultConnection = _conn;

      string entityName = DemoConfig.Default.entityName;
      string privaKeyFile = DemoConfig.Default.xmlPrivateKey;

      byte[] privateKey = File.ReadAllBytes(privaKeyFile);

      ComponentContext component =
        new DefaultComponentContext(new ComponentId("hello", 1, 0, 0, ".net"));
      component.AddFacet("Hello", Repository.GetRepositoryID(typeof (Hello)),
                         new HelloImpl(_conn));

      _conn.LoginByCertificate(entityName, privateKey);
      _conn.OnInvalidLogin = new HelloInvalidLoginCallback(entityName,
                                                           privateKey, manager);

      IComponent member = component.GetIComponent();
      ServiceProperty[] properties = new[] {
                                             new ServiceProperty("offer.domain",
                                                                 "Interoperability Tests")
                                           };
      _offer = _conn.Offers.registerService(member, properties);

      Console.WriteLine("Servidor no ar.");

      Thread.Sleep(Timeout.Infinite);
    }

    private static void CurrentDomainProcessExit(object sender, EventArgs e) {
      _offer.remove();
    }
  }
}