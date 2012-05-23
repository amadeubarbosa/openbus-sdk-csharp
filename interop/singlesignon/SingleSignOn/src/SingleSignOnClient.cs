using System;
using System.IO;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using omg.org.CORBA;
using tecgraf.openbus.core.v2_00.services.access_control;
using tecgraf.openbus.core.v2_00.services.offer_registry;
using tecgraf.openbus.interop.hello;
using tecgraf.openbus.interop.singlesignon.Properties;

namespace tecgraf.openbus.interop.singlesignon
{
  /// <summary>
  /// Cliente do demo hello.
  /// </summary>
  static class SingleSignOnClient {
    static void Main() {
      string hostName = DemoConfig.Default.hostName;
      short hostPort = DemoConfig.Default.hostPort;
      string secretFile = DemoConfig.Default.secretFile;
      string loginFile = DemoConfig.Default.loginFile;

      ConsoleAppender appender = new ConsoleAppender {
        Threshold = Level.Info,
        Layout = new SimpleLayout(),
      };
      BasicConfigurator.Configure(appender);

      ConnectionManager manager = ORBInitializer.Manager;
      Connection conn = manager.CreateConnection(hostName, hostPort);
      manager.DefaultConnection = conn;

      byte[] secret = File.ReadAllBytes(secretFile);
      string reference;
      using (FileStream fs = new FileStream(loginFile, FileMode.Open, FileAccess.Read)) {
        using (StreamReader sr = new StreamReader(fs)) {
          reference = sr.ReadToEnd();
        }
      }

      LoginProcess login =
        OrbServices.GetSingleton().string_to_object(reference) as LoginProcess;
      conn.LoginBySingleSignOn(login, secret);

      Console.WriteLine("Login por single sign on concluído, procurando faceta IHello.");

      // propriedades geradas automaticamente
      ServiceProperty autoProp1 = new ServiceProperty("openbus.offer.entity", "TestEntity");
      ServiceProperty autoProp2 = new ServiceProperty("openbus.component.facet", "Hello");
      // propriedade definida pelo servidor hello
      ServiceProperty prop = new ServiceProperty("offer.domain", "OpenBus Demos");

      ServiceProperty[] properties = new[] { autoProp1, autoProp2, prop};
      ServiceOfferDesc[] offers = conn.Offers.findServices(properties);

      if (offers.Length < 1) {
        Console.WriteLine("O serviço Hello não se encontra no barramento.");
        Environment.Exit(1);
      }
      if (offers.Length > 1)
        Console.WriteLine("Existe mais de um serviço Hello no barramento.");

      foreach (ServiceOfferDesc serviceOfferDesc in offers) {
        try {
          MarshalByRefObject helloObj = serviceOfferDesc.service_ref.getFacetByName("Hello");
          if (helloObj == null) {
            Console.WriteLine("Não foi possível encontrar uma faceta com esse nome.");
            continue;
          }
          Hello hello = helloObj as Hello;
          if (hello == null) {
            Console.WriteLine("Faceta encontrada não implementa IHello.");
            continue;
          }
          hello.sayHello();
        }
        catch (TRANSIENT) {
          Console.WriteLine("Uma das ofertas obtidas é de um cliente inativo. Tentando a próxima.");
        }
      }

      Console.WriteLine("Fim.");
      Console.ReadLine();
    }
  }
}
