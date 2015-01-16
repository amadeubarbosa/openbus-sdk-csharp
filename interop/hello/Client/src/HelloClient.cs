using System;
using System.Collections.Generic;
using System.Text;
using Ch.Elca.Iiop.Idl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using omg.org.CORBA;
using tecgraf.openbus.core.v2_1.services.offer_registry;
using tecgraf.openbus.interop.simple.Properties;
using tecgraf.openbus.interop.utils;

namespace tecgraf.openbus.interop.simple {
  /// <summary>
  /// Cliente do teste de interoperabilidade hello.
  /// </summary>
  [TestClass]
  internal static class HelloClient {
    private static void Main() {
      string hostName = DemoConfig.Default.busHostName;
      ushort hostPort = DemoConfig.Default.busHostPort;
      bool useSSL = DemoConfig.Default.useSSL;
      if (useSSL) {
        Utils.InitSSLORB();
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
      Connection conn = context.ConnectByAddress(hostName, hostPort, props);
      context.SetDefaultConnection(conn);

      const string userLogin = "interop_hello_csharp_client";
      byte[] userPassword = new ASCIIEncoding().GetBytes(userLogin);

      conn.LoginByPassword(userLogin, userPassword, "testing");

      // propriedades geradas automaticamente
      ServiceProperty autoProp =
        new ServiceProperty("openbus.component.interface",
                            Repository.GetRepositoryID(typeof (Hello)));
      // propriedade definida pelo servidor hello
      ServiceProperty prop = new ServiceProperty("offer.domain",
                                                 "Interoperability Tests");

      ServiceProperty[] properties = {autoProp, prop};
      List<ServiceOfferDesc> offers =
        Utils.FindOffer(ORBInitializer.Context.OfferRegistry, properties, 1, 10, 1);

      bool foundOne = false;
      foreach (ServiceOfferDesc serviceOfferDesc in offers) {
        try {
          MarshalByRefObject helloObj =
            serviceOfferDesc.service_ref.getFacet(
              Repository.GetRepositoryID(typeof (Hello)));
          if (helloObj == null) {
            Console.WriteLine(
              "Não foi possível encontrar uma faceta com esse nome.");
            continue;
          }
          Hello hello = helloObj as Hello;
          if (hello == null) {
            Console.WriteLine("Faceta encontrada não implementa Hello.");
            continue;
          }
          foundOne = true;
          Assert.AreEqual(hello.sayHello(), "Hello " + userLogin + "!");
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