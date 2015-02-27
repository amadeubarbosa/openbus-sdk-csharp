using System;
using System.Collections.Generic;
using System.Threading;
using Ch.Elca.Iiop.Idl;
using Scs.Core;
using omg.org.CORBA;
using scs.core;
using tecgraf.openbus.assistant;
using tecgraf.openbus.core.v2_0.services.access_control;
using tecgraf.openbus.core.v2_0.services.offer_registry;
using tecgraf.openbus.exceptions;
using tecgraf.openbus.interop.delegation.Properties;
using tecgraf.openbus.security;

namespace tecgraf.openbus.interop.delegation {
  /// <summary>
  /// Servidor broadcaster do teste de interoperabilidade delegation.
  /// </summary>
  internal static class BroadcasterServer {
    private const string Entity = "interop_delegation_csharp_broadcaster";
    private static PrivateKey _privateKey;
    private static IComponent _ic;
    private static ServiceProperty[] _properties;
    private static Connection _conn;
    private static ServiceOffer _offer;

    private static void Main() {
      AppDomain.CurrentDomain.ProcessExit += CurrentDomainProcessExit;
      string hostName = DemoConfig.Default.busHostName;
      ushort hostPort = DemoConfig.Default.busHostPort;
      _privateKey = Crypto.ReadKeyFile(DemoConfig.Default.privateKey);

      ConnectionProperties props = new ConnectionPropertiesImpl();
      props.AccessKey = _privateKey;
      OpenBusContext context = ORBInitializer.Context;
      _conn = context.CreateConnection(hostName, hostPort, props);
      context.SetDefaultConnection(_conn);

      _conn.LoginByCertificate(Entity, _privateKey);

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
      BroadcasterImpl broadcaster = new BroadcasterImpl(messenger);
      component.AddFacet("broadcaster",
                         Repository.GetRepositoryID(typeof (Broadcaster)),
                         broadcaster);

      _ic = component.GetIComponent();
      _properties = new[] {
                            new ServiceProperty("offer.domain",
                                                "Interoperability Tests")
                          };
      _offer = context.OfferRegistry.registerService(_ic, _properties);
      _conn.OnInvalidLogin = InvalidLogin;

      Console.WriteLine("Broadcaster no ar.");

      Thread.Sleep(Timeout.Infinite);
    }

    private static Messenger GetMessenger() {
      // propriedades geradas automaticamente
      ServiceProperty autoProp = new ServiceProperty(
        "openbus.component.interface",
        Repository.GetRepositoryID(typeof (Messenger)));
      // propriedade definida pelo servidor hello
      ServiceProperty prop = new ServiceProperty("offer.domain",
                                                 "Interoperability Tests");

      ServiceProperty[] properties = {autoProp, prop};
      List<ServiceOfferDesc> offers =
        Utils.FindOffer(ORBInitializer.Context.OfferRegistry, properties, 1, 10, 1);

      foreach (ServiceOfferDesc serviceOfferDesc in offers) {
        try {
          MarshalByRefObject messengerObj =
            serviceOfferDesc.service_ref.getFacet(Repository.GetRepositoryID(typeof(Messenger)));
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

    private static void InvalidLogin(Connection conn, LoginInfo login) {
      try {
        Console.WriteLine(
          "Callback de InvalidLogin foi chamada, tentando logar novamente no barramento.");
        conn.LoginByCertificate(Entity, _privateKey);
        _offer = ORBInitializer.Context.OfferRegistry.registerService(_ic,
                                                                      _properties);
      }
      catch (AlreadyLoggedInException) {
        // outra thread reconectou
      }
      catch (Exception e) {
        Console.WriteLine(e);
      }
    }

    private static void CurrentDomainProcessExit(object sender, EventArgs e) {
      if (_offer != null) {
        try {
          _offer.remove();
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