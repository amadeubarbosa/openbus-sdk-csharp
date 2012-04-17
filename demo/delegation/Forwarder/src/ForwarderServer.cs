﻿using System;
using System.Threading;
using Ch.Elca.Iiop.Idl;
using Forwarder.Properties;
using Scs.Core;
using omg.org.CORBA;
using scs.core;
using tecgraf.openbus.core.v2_00.services.offer_registry;
using tecgraf.openbus.demo.delegation;
using tecgraf.openbus.sdk;

namespace Forwarder {
  /// <summary>
  /// Servidor do demo hello.
  /// </summary>
  internal static class ForwarderServer {
    private static Connection _conn;
    private static ServiceOffer _offer;

    private static void Main(string[] args) {
      AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
      string hostName = DemoConfig.Default.hostName;
      int hostPort = DemoConfig.Default.hostPort;

      ConnectionManager manager = ORBInitializer.Manager;
      _conn = manager.CreateConnection(hostName, (short)hostPort);
      manager.DefaultConnection = _conn;

      string userLogin = DemoConfig.Default.userLogin;
      string userPassword = DemoConfig.Default.userPassword;
      System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
      byte[] password = encoding.GetBytes(userPassword);

      _conn.LoginByPassword(userLogin, password);

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
                         Repository.GetRepositoryID(
                           typeof (tecgraf.openbus.demo.delegation.Forwarder)),
                         forwarder);

      _conn.OnInvalidLoginCallback =
        new ForwarderInvalidLoginCallback(userLogin, password, forwarder);

      IComponent member = component.GetIComponent();
      ServiceProperty[] properties = new[] { new ServiceProperty("offer.domain",
                                                                 "OpenBus Demos")
                                           };
      _offer = _conn.OfferRegistry.registerService(member, properties);

      Console.WriteLine("Forwarder no ar.");

      Thread.Sleep(Timeout.Infinite);
    }

    private static Messenger GetMessenger() {
      // propriedades geradas automaticamente
      ServiceProperty autoProp1 = new ServiceProperty("openbus.offer.entity",
                                                      "messenger");
      ServiceProperty autoProp2 = new ServiceProperty(
        "openbus.component.facet", "messenger");
      // propriedade definida pelo servidor hello
      ServiceProperty prop = new ServiceProperty("offer.domain", "OpenBus Demos");

      ServiceProperty[] properties = new[] {autoProp1, autoProp2, prop};
      ServiceOfferDesc[] offers = _conn.OfferRegistry.findServices(properties);

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

    private static void CurrentDomain_ProcessExit(object sender, EventArgs e) {
      _offer.remove();
    }
  }
}