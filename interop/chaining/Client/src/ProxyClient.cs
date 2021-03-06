﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Ch.Elca.Iiop.Idl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using omg.org.CORBA;
using tecgraf.openbus.core.v2_1.services.offer_registry;
using tecgraf.openbus.interop.chaining.Properties;
using tecgraf.openbus.interop.utils;

namespace tecgraf.openbus.interop.chaining {
  /// <summary>
  /// Cliente do teste de interoperabilidade chaining.
  /// </summary>
  [TestClass]
  internal static class ProxyClient {
    private static void Main() {
      string hostName = DemoConfig.Default.busHostName;
      ushort hostPort = DemoConfig.Default.busHostPort;
      bool useSSL = DemoConfig.Default.useSSL;
      string clientUser = DemoConfig.Default.clientUser;
      string clientThumbprint = DemoConfig.Default.clientThumbprint;
      string serverUser = DemoConfig.Default.serverUser;
      string serverThumbprint = DemoConfig.Default.serverThumbprint;
      ushort serverSSLPort = DemoConfig.Default.serverSSLPort;
      ushort serverOpenPort = DemoConfig.Default.serverOpenPort;
      string busIORFile = DemoConfig.Default.busIORFile;
      if (useSSL) {
        Utils.InitSSLORB(clientUser, clientThumbprint, serverUser, serverThumbprint, serverSSLPort, serverOpenPort, true, true, "required", false, false);
      }
      else {
        ORBInitializer.InitORB();
      }

      ConsoleAppender appender = new ConsoleAppender {
        Threshold = Level.Fatal,
        Layout =
          new SimpleLayout(),
      };
      BasicConfigurator.Configure(appender);

      ConnectionProperties props = new ConnectionPropertiesImpl();
      OpenBusContext context = ORBInitializer.Context;
      Connection conn;
      if (useSSL) {
        string ior = File.ReadAllText(busIORFile);
        conn = context.ConnectByReference((MarshalByRefObject)OrbServices.CreateProxy(typeof(MarshalByRefObject), ior), props);
      }
      else {
        conn = context.ConnectByAddress(hostName, hostPort, props);
      }
      context.SetDefaultConnection(conn);

      const string userLogin = "interop_chaining_csharp_client";
      byte[] userPassword = new ASCIIEncoding().GetBytes(userLogin);

      conn.LoginByPassword(userLogin, userPassword, "testing");

      // propriedades geradas automaticamente
      ServiceProperty prop1 = new ServiceProperty("openbus.component.interface", Repository.GetRepositoryID(typeof(HelloProxy)));
      // propriedade definida pelo servidor hello
      ServiceProperty prop2 = new ServiceProperty("offer.domain",
                                                 "Interoperability Tests");

      ServiceProperty[] properties = { prop1, prop2 };
      List<ServiceOfferDesc> offers =
        Utils.FindOffer(ORBInitializer.Context.OfferRegistry, properties, 1, 10, 1);

      bool foundOne = false;
      foreach (ServiceOfferDesc serviceOfferDesc in offers) {
        try {
          string found = Utils.GetProperty(serviceOfferDesc.properties, "openbus.offer.entity");
          Console.WriteLine("Entidade encontrada: " + found);
          MarshalByRefObject helloProxyObj =
            serviceOfferDesc.service_ref.getFacet(
              Repository.GetRepositoryID(typeof(HelloProxy)));
          if (helloProxyObj == null) {
            Console.WriteLine(
              "Não foi possível encontrar uma faceta com esse nome.");
            continue;
          }
          HelloProxy helloProxy = helloProxyObj as HelloProxy;
          if (helloProxy == null) {
            Console.WriteLine("Faceta encontrada não implementa HelloProxy.");
            continue;
          }
          foundOne = true;
          String loginEntity = Utils.GetProperty(serviceOfferDesc.properties,
            "openbus.offer.entity");
          CallerChain chain = context.MakeChainFor(loginEntity);
          byte[] encodedChain = context.EncodeChain(chain);
          const string expected = "Hello " + userLogin + "!";
          Assert.AreEqual(helloProxy.fetchHello(encodedChain), expected);
        }
        catch (TRANSIENT) {
          Console.WriteLine(
            "Uma das ofertas obtidas é de um cliente inativo. Tentando a próxima.");
        }
      }
      conn.Logout();
      Assert.IsTrue(foundOne);
      Console.WriteLine("Fim.");
    }
  }
}
