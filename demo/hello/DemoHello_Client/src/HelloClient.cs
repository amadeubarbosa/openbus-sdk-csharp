using System;
using Client.Properties;
using demoidl.hello;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using omg.org.CORBA;
using tecgraf.openbus.core.v2_00.services.offer_registry;
using tecgraf.openbus.sdk;
using tecgraf.openbus.sdk.standard;

namespace tecgraf.openbus.demo.hello
{
  /// <summary>
  /// Cliente do demo hello.
  /// </summary>
  static class HelloClient {
    private static Connection _conn;

    static void Main(string[] args) {
      AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

      string hostName = DemoConfig.Default.hostName;
      int hostPort = DemoConfig.Default.hostPort;

      ConsoleAppender appender = new ConsoleAppender()
      {
        Threshold = Level.Info,
        Layout = new SimpleLayout(),
      };
      BasicConfigurator.Configure(appender);

      OpenBus openbus = StandardOpenBus.Instance;
      _conn = openbus.Connect(hostName, (short)hostPort);

      string userLogin = DemoConfig.Default.userLogin;
      string userPassword = DemoConfig.Default.userPassword;
      System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();

      _conn.LoginByPassword(userLogin, encoding.GetBytes(userPassword));

      Console.WriteLine("Pressione 'Enter' quando o servidor estiver no ar.");
      Console.ReadLine();

      // propriedades geradas automaticamente
      ServiceProperty autoProp1 = new ServiceProperty("openbus.offer.entity", "demo");
      ServiceProperty autoProp2 = new ServiceProperty("openbus.component.facet", "hello");
      // propriedade definida pelo servidor hello
      ServiceProperty prop = new ServiceProperty("offer.domain", "OpenBus Demos");

//      ServiceProperty[] properties = new[] {prop, autoProp1, autoProp2};
      ServiceProperty[] properties = new[] { new ServiceProperty("openbus.component.facet", "Hello"), };
      ServiceOfferDesc[] offers = _conn.OfferRegistry.findServices(properties);

      if (offers.Length < 1) {
        Console.WriteLine("O servi�o Hello n�o se encontra no barramento.");
        Environment.Exit(1);
      }
      if (offers.Length > 1)
        Console.WriteLine("Existe mais de um servi�o Hello no barramento.");

      foreach (ServiceOfferDesc serviceOfferDesc in offers) {
        try {
          MarshalByRefObject helloObj = serviceOfferDesc.service_ref.getFacet("IDL:demoidl/hello/IHello:1.0");
          if (helloObj == null) {
            Console.WriteLine("N�o foi poss�vel encontrar uma faceta com esse nome.");
            continue;
          }
          IHello hello = helloObj as IHello;
          if (hello == null) {
            Console.WriteLine("Faceta encontrada n�o implementa IHello.");
            continue;
          }
          hello.sayHello();
        }
        catch (TRANSIENT) {
          Console.WriteLine("Uma das ofertas obtidas � de um cliente inativo. Tentando a pr�xima.");
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
