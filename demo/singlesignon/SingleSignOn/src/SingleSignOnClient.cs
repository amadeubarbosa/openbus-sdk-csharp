using System;
using System.IO;
using SingleSignOn.Properties;
using demoidl.hello;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using omg.org.CORBA;
using tecgraf.openbus.core.v2_00.services.access_control;
using tecgraf.openbus.core.v2_00.services.offer_registry;
using tecgraf.openbus.sdk;
using tecgraf.openbus.sdk.standard;

namespace SingleSignOn {
  /// <summary>
  /// Cliente do demo hello.
  /// </summary>
  static class SingleSignOnClient {
    private static Connection _conn;

    static void Main(string[] args) {
      AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

      string hostName = DemoConfig.Default.hostName;
      int hostPort = DemoConfig.Default.hostPort;
      string secretFile = DemoConfig.Default.secretFile;
      string loginFile = DemoConfig.Default.loginFile;

      ConsoleAppender appender = new ConsoleAppender() {
        Threshold = Level.Info,
        Layout = new SimpleLayout(),
      };
      BasicConfigurator.Configure(appender);

      OpenBus openbus = StandardOpenBus.Instance;
      _conn = openbus.Connect(hostName, (short)hostPort);

      byte[] secret = File.ReadAllBytes(secretFile);
      string reference;
      using (FileStream fs = new FileStream(loginFile, FileMode.Open, FileAccess.Read)) {
        using (StreamReader sr = new StreamReader(fs)) {
          reference = sr.ReadToEnd();
        }
      }

      LoginProcess login =
        OrbServices.GetSingleton().string_to_object(reference) as LoginProcess;
      _conn.LoginBySingleSignOn(login, secret);

      Console.WriteLine("Login por single sign on concluído, procurando faceta IHello.");

      // propriedades geradas automaticamente
      ServiceProperty autoProp1 = new ServiceProperty("openbus.offer.entity", "demo");
      ServiceProperty autoProp2 = new ServiceProperty("openbus.component.facet", "hello");
      // propriedade definida pelo servidor hello
      ServiceProperty prop = new ServiceProperty("offer.domain", "OpenBus Demos");

      ServiceProperty[] properties = new[] { prop };//autoProp1, autoProp2, prop};
      ServiceOfferDesc[] offers = _conn.OfferRegistry.findServices(properties);

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
      }

      Console.WriteLine("Fim.");
      Console.ReadLine();
    }

    static void CurrentDomain_ProcessExit(object sender, EventArgs e) {
      _conn.Close();
    }
  }

}
