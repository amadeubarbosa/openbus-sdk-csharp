using System;
using System.IO;
using System.Threading;
using Ch.Elca.Iiop.Idl;
using Scs.Core;
using omg.org.CORBA;
using scs.core;
using tecgraf.openbus.core.v2_0.services.offer_registry;
using tecgraf.openbus.interop.delegation.Properties;

namespace tecgraf.openbus.interop.delegation {
  /// <summary>
  /// Servidor broadcaster do teste de interoperabilidade delegation.
  /// </summary>
  internal static class BroadcasterServer {
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
        new BroadcasterInvalidLoginCallback(entity, key);

      Messenger messenger = GetMessenger();
      if (messenger == null) {
        Console.WriteLine(
          "Não foi possível encontrar um Messenger no barramento.");
        Console.Read();
        return;
      }

      ComponentContext component =
        new DefaultComponentContext(new ComponentId("Broadcaster", 1, 0, 0,
                                                    ".net"));
      BroadcasterImpl broadcaster = new BroadcasterImpl(_conn, messenger);
      component.AddFacet("broadcaster", Repository.GetRepositoryID(typeof (Broadcaster)), broadcaster);

      IComponent member = component.GetIComponent();
      ServiceProperty[] properties = new[] {
                                             new ServiceProperty("offer.domain",
                                                                 "Interoperability Tests")
                                           };
      _offer = _conn.Offers.registerService(member, properties);

      Console.WriteLine("Broadcaster no ar.");

      Thread.Sleep(Timeout.Infinite);
    }

    private static Messenger GetMessenger() {
      // propriedades geradas automaticamente
      ServiceProperty autoProp1 = new ServiceProperty("openbus.offer.entity",
                                                      "interop_delegation_csharp_messenger");
      ServiceProperty autoProp2 = new ServiceProperty(
        "openbus.component.facet", "messenger");
      // propriedade definida pelo servidor hello
      ServiceProperty prop = new ServiceProperty("offer.domain", "Interoperability Tests");

      ServiceProperty[] properties = new[] {autoProp1, autoProp2, prop};
      ServiceOfferDesc[] offers = _conn.Offers.findServices(properties);

      if (offers.Length < 1) {
        Console.WriteLine("O serviço Messenger não se encontra no barramento.");
        Environment.Exit(1);
      }
      if (offers.Length > 1) {
        Console.WriteLine("Existe mais de um serviço Messenger no barramento.");
      }

      foreach (ServiceOfferDesc serviceOfferDesc in offers) {
        try {
          MarshalByRefObject messengerObj =
            serviceOfferDesc.service_ref.getFacetByName("messenger");
          if (messengerObj == null) {
            Console.WriteLine(
              "Não foi possível encontrar uma faceta com esse nome.");
            continue;
          }
          Messenger messenger = messengerObj as Messenger;
          if (messenger == null) {
            Console.WriteLine("Faceta encontrada não implementa Messenger.");
            continue;
          }
          return messenger;
        }
        catch (TRANSIENT) {
          Console.WriteLine(
            "Uma das ofertas obtidas é de um cliente inativo. Tentando a próxima.");
        }
      }
      return null;
    }

    private static void CurrentDomainProcessExit(object sender, EventArgs e) {
      if (_offer != null) {
        _offer.remove();
      }
    }
  }
}