using System;
using System.Collections.Generic;
using System.IO;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using omg.org.CORBA;
using tecgraf.openbus.core.v2_0.services.access_control;
using tecgraf.openbus.core.v2_0.services.offer_registry;
using tecgraf.openbus.interop.sharedauth.Properties;
using tecgraf.openbus.interop.simple;

namespace tecgraf.openbus.interop.sharedauth {
  /// <summary>
  /// Cliente do teste de interoperabilidade shared auth.
  /// </summary>
  internal static class SharedAuthClient {
    private static void Main() {
      string hostName = DemoConfig.Default.hostName;
      ushort hostPort = DemoConfig.Default.hostPort;
      string secretFile = DemoConfig.Default.secretFile;
      string loginFile = DemoConfig.Default.loginFile;

      ConsoleAppender appender = new ConsoleAppender {
                                                       Threshold = Level.Info,
                                                       Layout =
                                                         new SimpleLayout(),
                                                     };
      BasicConfigurator.Configure(appender);

      IDictionary<string, string> props = new Dictionary<string, string>();
      ConnectionManager manager = ORBInitializer.Manager;
      Connection conn = manager.CreateConnection(hostName, hostPort, props);
      manager.DefaultConnection = conn;

      byte[] secret = File.ReadAllBytes(secretFile);
      string reference;
      using (
        FileStream fs = new FileStream(loginFile, FileMode.Open, FileAccess.Read)
        ) {
        using (StreamReader sr = new StreamReader(fs)) {
          reference = sr.ReadToEnd();
        }
      }

      LoginProcess login =
        OrbServices.GetSingleton().string_to_object(reference) as LoginProcess;
      conn.LoginBySharedAuth(login, secret);

      Console.WriteLine(
        "Login por autenticação compartilhada concluído, procurando faceta Hello.");

      // propriedades geradas automaticamente
      ServiceProperty autoProp =
        new ServiceProperty("openbus.component.interface",
                            "IDL:tecgraf/openbus/interop/simple/Hello:1.0");
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