using System;
using System.Collections.Generic;
using System.Text;
using Ch.Elca.Iiop.Idl;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using omg.org.CORBA;
using tecgraf.openbus.core.v2_0.services.offer_registry;
using tecgraf.openbus.interop.simple.Properties;

namespace tecgraf.openbus.interop.simple {
  /// <summary>
  /// Cliente do teste de interoperabilidade hello.
  /// </summary>
  internal static class HelloClient {
    private static void Main() {
      string hostName = DemoConfig.Default.hostName;
      ushort hostPort = DemoConfig.Default.hostPort;

      ConsoleAppender appender = new ConsoleAppender {
                                                       Threshold = Level.All,
                                                       Layout =
                                                         new SimpleLayout(),
                                                     };
      BasicConfigurator.Configure(appender);

      IDictionary<string, string> props = new Dictionary<string, string>();
      ConnectionManager manager = ORBInitializer.Manager;
      Connection conn = manager.CreateConnection(hostName, hostPort, props);
      manager.DefaultConnection = conn;

      string userLogin = DemoConfig.Default.userLogin;
      byte[] userPassword =
        new ASCIIEncoding().GetBytes(DemoConfig.Default.userPassword);

      conn.LoginByPassword(userLogin, userPassword);

      Console.WriteLine("Pressione 'Enter' quando o servidor estiver no ar.");
      Console.ReadLine();

      // propriedades geradas automaticamente
      ServiceProperty autoProp =
        new ServiceProperty("openbus.component.interface",
                            Repository.GetRepositoryID(typeof (Hello)));
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

      foreach (ServiceOfferDesc serviceOfferDesc in offers) {
        try {
          MarshalByRefObject helloObj =
            serviceOfferDesc.service_ref.getFacet(
              "IDL:tecgraf/openbus/interop/simple/Hello:1.0");
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
          hello.sayHello();
        }
        catch (TRANSIENT) {
          Console.WriteLine(
            "Uma das ofertas obtidas é de um cliente inativo. Tentando a próxima.");
        }
      }
      conn.Logout();
      Console.WriteLine("Fim.");
    }
  }
}