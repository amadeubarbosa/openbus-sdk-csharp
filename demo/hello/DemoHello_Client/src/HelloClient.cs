using System;
using Client.Properties;
using demoidl.hello;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using scs.core;
using tecgraf.openbus.core.v1_05.registry_service;
using Tecgraf.Openbus;

namespace DemoHello_Client
{
  /// <summary>
  /// Cliente do demo hello.
  /// </summary>
  class HelloClient
  {
    private static Openbus openbus;

    static void Main(string[] args) {
      AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);

      string hostName = DemoConfig.Default.hostName;
      int hostPort = DemoConfig.Default.hostPort;

      ConsoleAppender appender = new ConsoleAppender()
      {
        Threshold = Level.Info,
        Layout = new SimpleLayout(),
      };
      BasicConfigurator.Configure(appender);

      openbus = Openbus.GetInstance();
      openbus.Init(hostName, hostPort);

      string userLogin = DemoConfig.Default.userLogin;
      string userPassword = DemoConfig.Default.userPassword;

      IRegistryService registryService = openbus.Connect(userLogin, userPassword);

      Console.WriteLine("Pressione 'Enter' quando o servidor estiver no ar.");
      Console.ReadLine();

      string[] facets = new string[] { "IHello" };
      ServiceOffer[] offers = registryService.find(facets);

      if (offers.Length < 1) {
        Console.WriteLine("O serviço Hello não se encontra no barramento.");
        Environment.Exit(1);
      }
      if (offers.Length > 1)
        Console.WriteLine("Existe mais de um serviço Hello no barramento.");

      IComponent component = offers[0].member;
      MarshalByRefObject helloObj = component.getFacetByName("IHello");

      if (helloObj == null) {
        Console.WriteLine("Não foi possível encontrar uma faceta com esse nome.");
        Environment.Exit(1);
      }

      IHello hello = helloObj as IHello;
      if (hello == null) {
        Console.WriteLine("Faceta encontrada não implementa IHello.");
        Environment.Exit(1);
      }

      hello.sayHello();

      Console.WriteLine("Fim.");
      Console.ReadLine();
    }

    static void CurrentDomain_ProcessExit(object sender, EventArgs e) {
      openbus.Disconnect();
      openbus.Destroy();
    }
  }

}
