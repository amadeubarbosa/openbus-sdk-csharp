using System;
using System.IO;
using Client.Properties;
using demoidl.hello;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using omg.org.CORBA;
using tecgraf.openbus.core.v2_00.services.access_control;
using tecgraf.openbus.core.v2_00.services.offer_registry;
using tecgraf.openbus.sdk;

namespace Client {
  /// <summary>
  /// Cliente do demo hello.
  /// </summary>
  static class HelloClient {
    static void Main() {
      string hostName = DemoConfig.Default.hostName;
      short hostPort = DemoConfig.Default.hostPort;

      ConsoleAppender appender = new ConsoleAppender {
        Threshold = Level.Info,
        Layout = new SimpleLayout(),
      };
      BasicConfigurator.Configure(appender);

      ConnectionManager manager = ORBInitializer.Manager;
      Connection conn = manager.CreateConnection(hostName, hostPort);
      manager.DefaultConnection = conn;

      string userLogin = DemoConfig.Default.userLogin;
      string userPassword = DemoConfig.Default.userPassword;
      string secretFile = DemoConfig.Default.secretFile;
      string loginFile = DemoConfig.Default.loginFile;
      System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();

      conn.LoginByPassword(userLogin, encoding.GetBytes(userPassword));
      byte[] secret;
      LoginProcess login = conn.StartSingleSignOn(out secret);

      using (FileStream fs = new FileStream(secretFile, FileMode.Create)) {
        using (BinaryWriter w = new BinaryWriter(fs)) {
          foreach (byte t in secret) {
            w.Write(t);
          }
        }
      }
      using (StreamWriter outfile = new StreamWriter(loginFile))
      {
        outfile.Write(OrbServices.GetSingleton().object_to_string(login));
      }

      Console.WriteLine("Chamando a faceta IHello por este cliente.");
      // propriedades geradas automaticamente
      ServiceProperty autoProp1 = new ServiceProperty("openbus.offer.entity", "TestEntity");
      ServiceProperty autoProp2 = new ServiceProperty("openbus.component.facet", "Hello");
      // propriedade definida pelo servidor hello
      ServiceProperty prop = new ServiceProperty("offer.domain", "OpenBus Demos");

      ServiceProperty[] properties = new[] { autoProp1, autoProp2, prop};
      ServiceOfferDesc[] offers = conn.OfferRegistry.findServices(properties);

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
          IHello hello = helloObj as IHello;
          if (hello == null) {
            Console.WriteLine("Faceta encontrada não implementa IHello.");
            continue;
          }
          hello.sayHello();
        }
        catch (TRANSIENT) {
          Console.WriteLine("Uma das ofertas obtidas é de um cliente inativo. Tentando a próxima.");
        }
        catch (OBJECT_NOT_EXIST) {
            Console.WriteLine("Uma das ofertas obtidas é de um cliente inativo. Tentando a próxima.");
        }
      }

      Console.WriteLine("Fim.");

      Console.WriteLine("Execute o cliente que fará o login por single sign on e, após seu término, pressione qualquer tecla para finalizar este cliente.");
      Console.ReadLine();
    }
  }
}
