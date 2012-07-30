using System;
using System.Collections.Generic;
using System.IO;
using Ch.Elca.Iiop.Idl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using omg.org.CORBA;
using omg.org.IOP;
using tecgraf.openbus.core.v2_0.services.access_control;
using tecgraf.openbus.core.v2_0.services.offer_registry;
using tecgraf.openbus.interop.sharedauth.Properties;
using tecgraf.openbus.interop.simple;
using TypeCode = omg.org.CORBA.TypeCode;

namespace tecgraf.openbus.interop.sharedauth {
  /// <summary>
  /// Cliente do teste de interoperabilidade shared auth.
  /// </summary>
  [TestClass]
  internal static class SharedAuthClient {
    private static void Main() {
      string hostName = DemoConfig.Default.busHostName;
      ushort hostPort = DemoConfig.Default.busHostPort;
      string loginFile = DemoConfig.Default.loginFile;

      ConsoleAppender appender = new ConsoleAppender {
                                                       Threshold = Level.Info,
                                                       Layout =
                                                         new SimpleLayout(),
                                                     };
      BasicConfigurator.Configure(appender);

      CodecFactory factory =
        OrbServices.GetSingleton().resolve_initial_references("CodecFactory") as
        CodecFactory;
      if (factory == null) {
        Assert.Fail("Impossível criar o codificador CDR.");
      }
      Codec codec =
        factory.create_codec(
          new Encoding(ENCODING_CDR_ENCAPS.ConstVal, 1, 2));

      IDictionary<string, string> props = new Dictionary<string, string>();
      ConnectionManager manager = ORBInitializer.Manager;
      Connection conn = manager.CreateConnection(hostName, hostPort, props);
      manager.DefaultConnection = conn;

      byte[] encodedLogin = File.ReadAllBytes(loginFile);
      Type saType = typeof (EncodedSharedAuth);
      TypeCode saTypeCode =
        OrbServices.GetSingleton().create_interface_tc(
          Repository.GetRepositoryID(saType), saType.Name);
      EncodedSharedAuth sharedAuth =
        (EncodedSharedAuth) codec.decode_value(encodedLogin, saTypeCode);

      LoginProcess login = sharedAuth.attempt as LoginProcess;
      conn.LoginBySharedAuth(login, sharedAuth.secret);

      Console.WriteLine(
        "Login por autenticação compartilhada concluído, procurando faceta Hello.");

      // propriedades geradas automaticamente
      ServiceProperty autoProp =
        new ServiceProperty("openbus.component.interface",
                            Repository.GetRepositoryID(typeof(Hello)));
      // propriedade definida pelo servidor hello
      ServiceProperty prop = new ServiceProperty("offer.domain",
                                                 "Interoperability Tests");

      ServiceProperty[] properties = new[] {autoProp, prop};
      ServiceOfferDesc[] offers = conn.Offers.findServices(properties);

      if (offers.Length < 1) {
        Console.WriteLine("O serviço Hello não se encontra no barramento.");
        Environment.Exit(1);
      }
      if (offers.Length > 1) {
        Console.WriteLine("Existe mais de um serviço Hello no barramento.");
      }

      bool foundOne = false;
      foreach (ServiceOfferDesc serviceOfferDesc in offers) {
        try {
          MarshalByRefObject helloObj =
            serviceOfferDesc.service_ref.getFacetByName("Hello");
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
          Assert.AreEqual(hello.sayHello(), "Hello " + conn.Login.Value.entity + "!");
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