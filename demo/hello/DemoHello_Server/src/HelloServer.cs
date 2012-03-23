using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using Ch.Elca.Iiop.Idl;
using Scs.Core;
using Server;
using log4net.Config;
using Server.Properties;
using scs.core;
using tecgraf.openbus.core.v2_00.services.offer_registry;
using tecgraf.openbus.sdk;
using tecgraf.openbus.sdk.Security;
using tecgraf.openbus.sdk.Standard;

namespace tecgraf.openbus.demo.hello {
  /// <summary>
  /// Servidor do demo hello.
  /// </summary>
  static class HelloServer {
    //TODO: depois que colocar o getconnection (ou equivalente) no sdk, remover essas globais
    public static Connection Conn;
    private static ServiceOffer _offer;
    static void Main(string[] args) {
      AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
      string hostName = DemoConfig.Default.hostName;
      int hostPort = DemoConfig.Default.hostPort;

      FileInfo logFileInfo = new FileInfo(DemoConfig.Default.logFile);
      XmlConfigurator.ConfigureAndWatch(logFileInfo);

      OpenBus openbus = StandardOpenBus.Instance;
      Conn = openbus.Connect(hostName, (short)hostPort);

      string entityName = DemoConfig.Default.entityName;
      string privaKeyFile = DemoConfig.Default.xmlPrivateKey;

      byte[] privateKey = File.ReadAllBytes(privaKeyFile);

      ComponentContext component =
        new DefaultComponentContext(new ComponentId("hello", 1, 0, 0, ".net"));
      component.AddFacet("hello", Repository.GetRepositoryID(typeof(IHello)), new HelloImpl(component, Conn));

      Conn.LoginByCertificate(entityName, privateKey);
      Conn.OnInvalidLoginCallback = new HelloInvalidLoginCallback(entityName, privateKey);

      IComponent member = component.GetIComponent();
      ServiceProperty[] properties = new[] {new ServiceProperty("offer.domain", "OpenBus Demos")};
      _offer = Conn.OfferRegistry.registerService(member, properties);

      Console.WriteLine("Servidor no ar.");

      Thread.Sleep(Timeout.Infinite);
    }

    static void CurrentDomain_ProcessExit(object sender, EventArgs e) {
      _offer.remove();
      Conn.Close();
    }
  }
}
