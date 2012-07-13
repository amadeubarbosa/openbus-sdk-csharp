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
  /// Servidor forwarder do teste de interoperabilidade delegation.
  /// </summary>
  internal static class ForwarderServer {
    private static Connection _conn;
    internal static ServiceOffer Offer;

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

      Messenger messenger = GetMessenger();
      if (messenger == null) {
        Console.WriteLine(
          "Não foi possível encontrar um Messenger no barramento.");
        Console.Read();
        return;
      }

      ComponentContext component =
        new DefaultComponentContext(new ComponentId("Forwarder", 1, 0, 0, ".net"));
      ForwarderImpl forwarder = new ForwarderImpl(_conn, messenger);
      component.AddFacet("forwarder",
                         Repository.GetRepositoryID(typeof (Forwarder)),
                         forwarder);

      IComponent ic = component.GetIComponent();
      ServiceProperty[] properties = new[] {
                                             new ServiceProperty("offer.domain",
                                                                 "Interoperability Tests")
                                           };
      Offer = _conn.Offers.registerService(ic, properties);
      _conn.OnInvalidLogin =
        new ForwarderInvalidLoginCallback(entity, key, ic, properties, forwarder);


      Console.WriteLine("Forwarder no ar.");

      Thread.Sleep(Timeout.Infinite);
    }

    private static Messenger GetMessenger() {
      // propriedades geradas automaticamente
      ServiceProperty autoProp1 = new ServiceProperty("openbus.offer.entity",
                                                      "interop_delegation_csharp_messenger");
      ServiceProperty autoProp2 = new ServiceProperty(
        "openbus.component.facet", "messenger");
      // propriedade definida pelo servidor hello
      ServiceProperty prop = new ServiceProperty("offer.domain",
                                                 "Interoperability Tests");

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
      if (Offer != null) {
        try {
          Offer.remove();
        }
        catch (Exception exc) {
          Console.WriteLine(
            "Erro ao remover a oferta antes de finalizar o processo: ", exc);
        }
      }
      _conn.Logout();
    }
  }
}