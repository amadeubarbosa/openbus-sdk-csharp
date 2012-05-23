using System;
using omg.org.CORBA;
using tecgraf.openbus.core.v2_00.services.offer_registry;

namespace tecgraf.openbus.demo.hello
{
  /// <summary>
  /// Cliente do demo hello.
  /// </summary>
  static class HelloClient {
    static void Main() {
      ConnectionManager manager = ORBInitializer.Manager;
      Connection conn = manager.CreateConnection("ubu", 2089);
      manager.DefaultConnection = conn;

      System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();

      conn.LoginByPassword("tester", encoding.GetBytes("tester"));

      Console.WriteLine("Pressione 'Enter' quando o servidor estiver no ar.");
      Console.ReadLine();

      // propriedades geradas automaticamente
      ServiceProperty autoProp1 = new ServiceProperty("openbus.offer.entity", "TestEntity");
      ServiceProperty autoProp2 = new ServiceProperty("openbus.component.facet", "Hello");
      // propriedade definida pelo servidor hello
      ServiceProperty prop = new ServiceProperty("offer.domain", "OpenBus Demos");

      ServiceProperty[] properties = new[] {prop, autoProp1, autoProp2};
      ServiceOfferDesc[] offers = conn.Offers.findServices(properties);

      if (offers.Length < 1) {
        Console.WriteLine("O serviço Hello não se encontra no barramento.");
        Environment.Exit(1);
      }
      if (offers.Length > 1)
        Console.WriteLine("Existe mais de um serviço Hello no barramento.");

      foreach (ServiceOfferDesc serviceOfferDesc in offers) {
        try {
          MarshalByRefObject helloObj = serviceOfferDesc.service_ref.getFacet("IDL:demoidl/hello/IHello:1.0");
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
