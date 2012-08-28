using System;
using System.Collections.Generic;
using System.Threading;
using Ch.Elca.Iiop.Idl;
using Scs.Core;
using scs.core;
using tecgraf.openbus.core.v2_0.services.offer_registry;
using tecgraf.openbus.interop.delegation.Properties;
using tecgraf.openbus.security;

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
      PrivateKey key = Crypto.ReadKeyFile(DemoConfig.Default.privateKey);

      IDictionary<string, string> props = new Dictionary<string, string>();
      OpenBusContext context = ORBInitializer.Context;
      _conn = context.CreateConnection(hostName, hostPort, props);
      context.SetDefaultConnection(_conn);

      const string entity = "interop_delegation_csharp_messenger";

      _conn.LoginByCertificate(entity, key);

      ComponentContext component =
        new DefaultComponentContext(new ComponentId("Messenger", 1, 0, 0, ".net"));
      MessengerImpl messenger = new MessengerImpl();
      component.AddFacet("messenger",
                         Repository.GetRepositoryID(typeof (Messenger)),
                         messenger);

      IComponent ic = component.GetIComponent();
      ServiceProperty[] properties = new[] {
                                             new ServiceProperty("offer.domain",
                                                                 "Interoperability Tests")
                                           };
      Offer = context.OfferRegistry.registerService(ic, properties);
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