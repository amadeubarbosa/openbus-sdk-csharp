using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Remoting;
using System.Text;
using Ch.Elca.Iiop.Idl;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using omg.org.CORBA;
using scs.core;
using tecgraf.openbus.core.v2_1.services.offer_registry;
using tecgraf.openbus.interop.multiplexing.Properties;
using tecgraf.openbus.interop.simple;
using tecgraf.openbus.interop.utils;

namespace tecgraf.openbus.interop.multiplexing {
  [TestClass]
  internal static class Client {
    private static readonly ILog Logger =
      LogManager.GetLogger(typeof(Client));

    private static void Main() {
      string hostName = DemoConfig.Default.busHostName;
      ushort hostPort = DemoConfig.Default.busHostPort;
      ushort hostPort2 = DemoConfig.Default.bus2HostPort;
      ASCIIEncoding encoding = new ASCIIEncoding();
      object[] buses;
      bool useSSL = DemoConfig.Default.useSSL;
      string clientUser = DemoConfig.Default.clientUser;
      string clientThumbprint = DemoConfig.Default.clientThumbprint;
      string serverUser = DemoConfig.Default.serverUser;
      string serverThumbprint = DemoConfig.Default.serverThumbprint;
      string serverSSLPort = DemoConfig.Default.serverSSLPort;
      string busIORFile = DemoConfig.Default.busIORFile;
      string bus2IORFile = DemoConfig.Default.bus2IORFile;
      if (useSSL) {
        Utils.InitSSLORB(clientUser, clientThumbprint, serverUser, serverThumbprint, serverSSLPort);
        buses = new object[] { busIORFile, bus2IORFile };
      }
      else {
        ORBInitializer.InitORB();
        buses = new object[] { hostPort, hostPort2 };
      }

      ConnectionProperties props = new ConnectionPropertiesImpl();
      OpenBusContext context = ORBInitializer.Context;

      for (int i = 0; i < buses.Length; i++) {
        Connection conn;
        if (useSSL) {
          string ior = File.ReadAllText((string)buses[i]);
          conn = context.ConnectByReference((IComponent)RemotingServices.Connect(typeof(IComponent), ior), props);
        }
        else {
          conn = context.ConnectByAddress(hostName, (ushort)buses[i], props);
        }
        context.SetDefaultConnection(conn);
        const string login = "interop_multiplexing_csharp_client";
        conn.LoginByPassword(login, encoding.GetBytes(login), "testing");

        ServiceProperty[] properties = new ServiceProperty[2];
        properties[0] =
          new ServiceProperty("openbus.component.interface",
                              Repository.GetRepositoryID(typeof (Hello)));
        properties[1] = new ServiceProperty("offer.domain",
                                                   "Interoperability Tests");
        List<ServiceOfferDesc> offers =
          Utils.FindOffer(ORBInitializer.Context.OfferRegistry, properties, 1, 10, 1);

        foreach (ServiceOfferDesc offer in offers) {
          string entity = Utils.GetProperty(offer.properties, "openbus.offer.entity");
          if (entity != null) {
            Logger.Info("found offer from " + entity + " on bus " + i);
          }
          try {
            MarshalByRefObject obj =
              offer.service_ref.getFacet(
                Repository.GetRepositoryID(typeof (Hello)));
            if (obj == null) {
              Logger.Info(
                "Não foi possível encontrar uma faceta com esse nome.");
              continue;
            }
            Hello hello = obj as Hello;
            if (hello == null) {
              Logger.Info("Faceta encontrada não implementa Hello.");
              continue;
            }
            string expected = String.Format("Hello {0}@{1}!", login,
                                            conn.BusId);
            string ret = hello.sayHello();
            Assert.AreEqual(expected, ret);
          }
          catch (TRANSIENT) {
            Logger.Info(
              "Uma das ofertas obtidas é de um cliente inativo. Tentando a próxima.");
          }
        }
        conn.Logout();
      }
    }
  }
}