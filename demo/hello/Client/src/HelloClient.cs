using System;
using System.Collections.Generic;
using omg.org.CORBA;
using tecgraf.openbus.core.v2_00.services;
using tecgraf.openbus.core.v2_00.services.access_control;
using tecgraf.openbus.core.v2_00.services.offer_registry;
using tecgraf.openbus.exceptions;

namespace tecgraf.openbus.demo.hello {
  /// <summary>
  /// Cliente da demo hello.
  /// </summary>
  internal static class HelloClient {
    private static void Main() {
      // Cria conex�o e a define como conex�o padr�o tanto para entrada como sa�da.
      // O uso exclusivo da conex�o padr�o (sem uso de requester e dispatcher) s� � recomendado para aplica��es que criem apenas uma conex�o e desejem utiliz�-la em todos os casos. Para situa��es diferentes, consulte o manual do SDK OpenBus e/ou outras demos.
      ConnectionManager manager = ORBInitializer.Manager;
      Connection conn = manager.CreateConnection("localhost", 2089);
      manager.DefaultConnection = conn;

      // Faz o login
      System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
      if (!Login("tester", encoding.GetBytes("tester"), conn)) {
        Console.ReadLine();
        Environment.Exit(1);
      }

      // Faz busca utilizando propriedades geradas automaticamente e propriedades definidas pelo servi�o espec�fico
      // propriedades geradas automaticamente
      ServiceProperty autoProp1 = new ServiceProperty("openbus.offer.entity",
                                                      "TestEntity");
      ServiceProperty autoProp2 = new ServiceProperty(
        "openbus.component.facet", "Hello");
      // propriedade definida pelo servi�o hello
      ServiceProperty prop = new ServiceProperty("offer.domain", "OpenBus Demos");
      ServiceProperty[] properties = new[] {prop, autoProp1, autoProp2};
      ServiceOfferDesc[] offers = Find(properties, conn);
      if (offers == null) {
        Console.ReadLine();
        Environment.Exit(1);
      }

      // analiza as ofertas encontradas
      Hello hello = GetHello(offers);
      if (hello == null) {
        Console.ReadLine();
        Environment.Exit(1);
      }

      // utiliza o servi�o
      hello.sayHello();

      Console.WriteLine("Fim.");
      Console.ReadLine();
    }

    private static Hello GetHello(ICollection<ServiceOfferDesc> offers) {
      if (offers.Count < 1) {
        Console.WriteLine("O servi�o Hello n�o se encontra no barramento.");
        return null;
      }

      if (offers.Count > 1) {
        Console.WriteLine(
          "Existe mais de um servi�o Hello no barramento. Tentaremos encontrar uma funcional.");
      }
      foreach (ServiceOfferDesc serviceOfferDesc in offers) {
        Console.WriteLine("Testando uma das ofertas recebidas...");
        try {
          MarshalByRefObject helloObj =
            serviceOfferDesc.service_ref.getFacet(
              "IDL:tecgraf/openbus/demo/hello/Hello:1.0");
          if (helloObj == null) {
            Console.WriteLine(
              "N�o foi poss�vel encontrar uma faceta Hello na oferta.");
            continue;
          }
          Hello hello = helloObj as Hello;
          if (hello == null) {
            Console.WriteLine(
              "Faceta encontrada na oferta n�o implementa Hello.");
            continue;
          }
          Console.WriteLine(
            "Foi encontrada uma oferta com um servi�o funcional.");
          return hello;
        }
        catch (TRANSIENT) {
          Console.WriteLine(
            "A oferta � de um cliente inativo. Tentando a pr�xima.");
        }
      }
      Console.WriteLine(
        "N�o foi encontrada uma oferta com um servi�o funcional.");
      return null;
    }

    private static ServiceOfferDesc[] Find(ServiceProperty[] properties,
                                           Connection conn) {
      try {
        return conn.Offers.findServices(properties);
      }
      catch (ServiceFailure e) {
        Console.WriteLine(
          "Erro ao tentar realizar a busca por um servi�o no barramento: Falha no servi�o remoto. Causa:");
        Console.WriteLine(e.StackTrace);
      }
      catch (Exception e) {
        Console.WriteLine(
          "Erro inesperado ao tentar realizar a busca por um servi�o no barramento:");
        Console.WriteLine(e.StackTrace);
      }
      return null;
    }

    private static bool Login(string login, byte[] password, Connection conn) {
      try {
        conn.LoginByPassword(login, password);
      }
      catch (AlreadyLoggedInException) {
        Console.WriteLine(
          "Falha ao tentar realizar o login por senha no barramento: a entidade j� est� com o login realizado. Esta falha ser� ignorada.");
        return true;
      }
      catch (AccessDenied) {
        Console.WriteLine(
          "Erro ao tentar realizar o login por senha no barramento: a senha fornecida n�o foi validada para a entidade " +
          login + ".");
      }
      catch (ServiceFailure e) {
        Console.WriteLine(
          "Erro ao tentar realizar o login por senha no barramento: Falha no servi�o remoto. Causa:");
        Console.WriteLine(e.StackTrace);
      }
      catch (Exception e) {
        Console.WriteLine(
          "Erro inesperado ao tentar realizar o login por senha no barramento:");
        Console.WriteLine(e.StackTrace);
      }
      return false;
    }
  }
}