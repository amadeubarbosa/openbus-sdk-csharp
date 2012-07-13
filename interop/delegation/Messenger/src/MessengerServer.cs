using System;
using System.IO;
using System.Threading;
using Ch.Elca.Iiop.Idl;
using Scs.Core;
using scs.core;
using tecgraf.openbus.core.v2_0.services.offer_registry;
using tecgraf.openbus.interop.delegation.Properties;

namespace tecgraf.openbus.interop.delegation {
  /// <summary>
  /// Servidor messenger do teste de interoperabilidade delegation.
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

      string entity = DemoConfig.Default.entity;
      string privateKey = DemoConfig.Default.privateKey;
      byte[] key = File.ReadAllBytes(privateKey);

      _conn.LoginByCertificate(entity, key);
      _conn.OnInvalidLogin =
        new MessengerInvalidLoginCallback(entity, key);

      ComponentContext component =
        new DefaultComponentContext(new ComponentId("Messenger", 1, 0, 0, ".net"));
      MessengerImpl messenger = new MessengerImpl(_conn);
      component.AddFacet("messenger",
                         Repository.GetRepositoryID(typeof (Messenger)),
                         messenger);

      IComponent member = component.GetIComponent();
      ServiceProperty[] properties = new[] {
                                             new ServiceProperty("offer.domain",
                                                                 "Interoperability Tests")
                                           };
      _offer = _conn.Offers.registerService(member, properties);

      Console.WriteLine("Messenger no ar.");

      Thread.Sleep(Timeout.Infinite);
    }

    private static void CurrentDomainProcessExit(object sender, EventArgs e) {
      if (_offer != null) {
        _offer.remove();
      }
    }
  }
}