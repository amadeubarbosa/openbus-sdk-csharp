using System;
using System.Threading;
using Ch.Elca.Iiop.Idl;
using Scs.Core;
using scs.core;
using tecgraf.openbus.core.v2_00.services.offer_registry;
using tecgraf.openbus.interop.delegation.Properties;

namespace tecgraf.openbus.interop.delegation {
  /// <summary>
  /// Servidor do demo hello.
  /// </summary>
  internal static class MessengerServer {
    private static Connection _conn;
    private static ServiceOffer _offer;

    private static void Main() {
      AppDomain.CurrentDomain.ProcessExit += CurrentDomainProcessExit;
      string hostName = DemoConfig.Default.hostName;
      short hostPort = DemoConfig.Default.hostPort;

      ConnectionManager manager = ORBInitializer.Manager;
      _conn = manager.CreateConnection(hostName, hostPort);
      manager.DefaultConnection = _conn;

      string userLogin = DemoConfig.Default.userLogin;
      string userPassword = DemoConfig.Default.userPassword;
      System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
      byte[] password = encoding.GetBytes(userPassword);

      _conn.LoginByPassword(userLogin, password);
      _conn.OnInvalidLoginCallback =
        new MessengerInvalidLoginCallback(userLogin, password);

      ComponentContext component =
        new DefaultComponentContext(new ComponentId("Messenger", 1, 0, 0, ".net"));
      MessengerImpl messenger = new MessengerImpl(_conn);
      component.AddFacet("messenger", Repository.GetRepositoryID(typeof(Messenger)), messenger);

      IComponent member = component.GetIComponent();
      ServiceProperty[] properties = new[] { new ServiceProperty("offer.domain",
                                                                 "OpenBus Demos")
                                           };
      _offer = _conn.Offers.registerService(member, properties);

      Console.WriteLine("Messenger no ar.");

      Thread.Sleep(Timeout.Infinite);
    }

    private static void CurrentDomainProcessExit(object sender, EventArgs e) {
      _offer.remove();
    }
  }
}