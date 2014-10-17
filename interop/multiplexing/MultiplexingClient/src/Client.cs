using System;
using System.Text;
using Ch.Elca.Iiop.Idl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using omg.org.CORBA;
using tecgraf.openbus.core.v2_1.services.offer_registry;
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

        ServiceProperty[] serviceProperties = new ServiceProperty[2];
        serviceProperties[0] =
          new ServiceProperty("openbus.component.interface",
                              Repository.GetRepositoryID(typeof (Hello)));
        serviceProperties[1] = new ServiceProperty("offer.domain",
                                                   "Interoperability Tests");
        ServiceOfferDesc[] services =
          context.OfferRegistry.findServices(serviceProperties);
        foreach (ServiceOfferDesc offer in services) {
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