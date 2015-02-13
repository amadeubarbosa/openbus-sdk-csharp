﻿using System;
using System.Collections.Generic;
using System.Text;
using Ch.Elca.Iiop.Idl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using omg.org.CORBA;
using tecgraf.openbus.assistant;
using tecgraf.openbus.core.v2_0.services.offer_registry;
using tecgraf.openbus.interop.multiplexing.Properties;
using tecgraf.openbus.interop.simple;

namespace tecgraf.openbus.interop.multiplexing {
  [TestClass]
  internal static class Client {
    private static void Main() {
      string hostName = DemoConfig.Default.busHostName;
      ushort hostPort = DemoConfig.Default.busHostPort;
      ushort hostPort2 = DemoConfig.Default.bus2HostPort;
      ASCIIEncoding encoding = new ASCIIEncoding();
      ushort[] ports = {hostPort, hostPort2};

      ConnectionProperties props = new ConnectionPropertiesImpl();
      OpenBusContext context = ORBInitializer.Context;

      foreach (ushort port in ports) {
        Connection conn = context.CreateConnection(hostName, port, props);
        context.SetDefaultConnection(conn);
        const string login = "interop_multiplexing_csharp_client";
        conn.LoginByPassword(login, encoding.GetBytes(login));

        ServiceProperty[] properties = new ServiceProperty[2];
        properties[0] =
          new ServiceProperty("openbus.component.interface",
                              Repository.GetRepositoryID(typeof (Hello)));
        properties[1] = new ServiceProperty("offer.domain",
                                                   "Interoperability Tests");
        List<ServiceOfferDesc> offers =
          Utils.FindOffer(ORBInitializer.Context.OfferRegistry, properties, 1, 10, 1);
        foreach (ServiceOfferDesc offer in offers) {
          foreach (ServiceProperty prop in offer.properties) {
            if (prop.name.Equals("openbus.offer.entity")) {
              Console.WriteLine("found offer from " + prop.value +
                                " on bus at port " + port);
            }
          }
          try {
            MarshalByRefObject obj =
              offer.service_ref.getFacet(
                Repository.GetRepositoryID(typeof (Hello)));
            if (obj == null) {
              Console.WriteLine(
                "Não foi possível encontrar uma faceta com esse nome.");
              continue;
            }
            Hello hello = obj as Hello;
            if (hello == null) {
              Console.WriteLine("Faceta encontrada não implementa Hello.");
              continue;
            }
            string expected = String.Format("Hello {0}@{1}!", login,
                                            conn.BusId);
            string ret = hello.sayHello();
            Assert.AreEqual(expected, ret);
          }
          catch (TRANSIENT) {
            Console.WriteLine(
              "Uma das ofertas obtidas é de um cliente inativo. Tentando a próxima.");
          }
        }
        conn.Logout();
      }
    }
  }
}