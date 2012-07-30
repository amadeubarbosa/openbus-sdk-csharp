using System;
using System.Collections.Generic;
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
    internal static ServiceOffer Offer;

    private static void Main() {
      AppDomain.CurrentDomain.ProcessExit += CurrentDomainProcessExit;
      string hostName = DemoConfig.Default.busHostName;
      ushort hostPort = DemoConfig.Default.busHostPort;

      IDictionary<string, string> props = new Dictionary<string, string>();
      ConnectionManager manager = ORBInitializer.Manager;
      _conn = manager.CreateConnection(hostName, hostPort, props);
      manager.DefaultConnection = _conn;

      const string entity = "interop_delegation_csharp_messenger";
      string privateKey = DemoConfig.Default.privateKey;
      byte[] key = File.ReadAllBytes(privateKey);

      _conn.LoginByCertificate(entity, key);

      ComponentContext component =
        new DefaultComponentContext(new ComponentId("Messenger", 1, 0, 0, ".net"));
      MessengerImpl messenger = new MessengerImpl(_conn);
      component.AddFacet("messenger",
                         Repository.GetRepositoryID(typeof (Messenger)),
                         messenger);

      IComponent ic = component.GetIComponent();
      ServiceProperty[] properties = new[] {
                                             new ServiceProperty("offer.domain",
                                                                 "Interoperability Tests")
                                           };
      Offer = _conn.Offers.registerService(ic, properties);
      _conn.OnInvalidLogin =
        new MessengerInvalidLoginCallback(entity, key, ic, properties);

      Console.WriteLine("Messenger no ar.");

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