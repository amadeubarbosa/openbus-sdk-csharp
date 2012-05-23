using System;
using System.IO;
using System.Threading;
using Ch.Elca.Iiop.Idl;
using Scs.Core;
using scs.core;
using tecgraf.openbus.core.v2_00.services.offer_registry;

namespace tecgraf.openbus.demo.hello {
  /// <summary>
  /// Servidor do demo hello.
  /// </summary>
  static class HelloServer {
    private static Connection _conn;
    private static ServiceOffer _offer;
    static void Main() {
      AppDomain.CurrentDomain.ProcessExit += CurrentDomainProcessExit;

      ConnectionManager manager = ORBInitializer.Manager;
      _conn = manager.CreateConnection("ubu", 2089);
      manager.DefaultConnection = _conn;

      byte[] privateKey = File.ReadAllBytes("DemoHello.key");

      ComponentContext component =
        new DefaultComponentContext(new ComponentId("hello", 1, 0, 0, ".net"));
      component.AddFacet("Hello", Repository.GetRepositoryID(typeof(Hello)), new HelloImpl(_conn));

      _conn.LoginByCertificate("HelloServer", privateKey);
      _conn.OnInvalidLoginCallback = new HelloInvalidLoginCallback("HelloServer", privateKey, manager);

      IComponent member = component.GetIComponent();
      ServiceProperty[] properties = new[] {new ServiceProperty("offer.domain", "OpenBus Demos")};
      _offer = _conn.Offers.registerService(member, properties);

      Console.WriteLine("Servidor no ar.");

      Thread.Sleep(Timeout.Infinite);
    }

    static void CurrentDomainProcessExit(object sender, EventArgs e) {
      _offer.remove();
    }
  }
}
