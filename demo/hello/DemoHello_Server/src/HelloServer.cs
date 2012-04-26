using System;
using System.IO;
using System.Threading;
using Ch.Elca.Iiop.Idl;
using Scs.Core;
using Server;
using demoidl.hello;
using log4net.Config;
using Server.Properties;
using scs.core;
using tecgraf.openbus.core.v2_00.services.offer_registry;
using tecgraf.openbus.sdk;

namespace tecgraf.openbus.demo.hello {
  /// <summary>
  /// Servidor do demo hello.
  /// </summary>
  static class HelloServer {
    private static Connection _conn;
    private static ServiceOffer _offer;
    static void Main() {
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
      component.AddFacet("Hello", Repository.GetRepositoryID(typeof(IHello)), new HelloImpl(_conn));

      _conn.LoginByCertificate(entityName, privateKey);
      _conn.OnInvalidLoginCallback = new HelloInvalidLoginCallback(entityName, privateKey, manager);

      IComponent member = component.GetIComponent();
      ServiceProperty[] properties = new[] {new ServiceProperty("offer.domain", "OpenBus Demos")};
      _offer = _conn.OfferRegistry.registerService(member, properties);

      Console.WriteLine("Servidor no ar.");

      Thread.Sleep(Timeout.Infinite);
    }

    static void CurrentDomainProcessExit(object sender, EventArgs e) {
      _offer.remove();
    }
  }
}
