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
      // Cria conexão e a define como conexão padrão tanto para entrada como saída.
      // O uso exclusivo da conexão padrão (sem uso de requester e dispatcher) só é recomendado para aplicações que criem apenas uma conexão e desejem utilizá-la em todos os casos. Para situações diferentes, consulte o manual do SDK OpenBus e/ou outras demos.
      ConnectionManager manager = ORBInitializer.Manager;
      Connection conn = manager.CreateConnection("localhost", 2089);
      manager.DefaultConnection = conn;

      // Faz o login
      System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
      if (!Login("tester", encoding.GetBytes("tester"), conn)) {
        Console.ReadLine();
        Environment.Exit(1);
      }

      // Faz busca utilizando propriedades geradas automaticamente e propriedades definidas pelo serviço específico
      // propriedades geradas automaticamente
      ServiceProperty autoProp1 = new ServiceProperty("openbus.offer.entity",
                                                      "TestEntity");
      ServiceProperty autoProp2 = new ServiceProperty(
        "openbus.component.facet", "Hello");
      // propriedade definida pelo serviço hello
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

      // utiliza o serviço
      hello.sayHello();

      Console.WriteLine("Fim.");
      Console.ReadLine();
    }

    private static Hello GetHello(ICollection<ServiceOfferDesc> offers) {
      if (offers.Count < 1) {
        Console.WriteLine("O serviço Hello não se encontra no barramento.");
        return null;
      }

      if (offers.Count > 1) {
        Console.WriteLine(
          "Existe mais de um serviço Hello no barramento. Tentaremos encontrar uma funcional.");
      }
      foreach (ServiceOfferDesc serviceOfferDesc in offers) {
        Console.WriteLine("Testando uma das ofertas recebidas...");
        try {
          MarshalByRefObject helloObj =
            serviceOfferDesc.service_ref.getFacet(
              "IDL:tecgraf/openbus/demo/hello/Hello:1.0");
          if (helloObj == null) {
            Console.WriteLine(
              "Não foi possível encontrar uma faceta Hello na oferta.");
            continue;
          }
          Hello hello = helloObj as Hello;
          if (hello == null) {
            Console.WriteLine(
              "Faceta encontrada na oferta não implementa Hello.");
            continue;
          }
          Console.WriteLine(
            "Foi encontrada uma oferta com um serviço funcional.");
          return hello;
        }
        catch (TRANSIENT) {
          Console.WriteLine(
            "A oferta é de um cliente inativo. Tentando a próxima.");
        }
      }
      Console.WriteLine(
        "Não foi encontrada uma oferta com um serviço funcional.");
      return null;
    }

    private static ServiceOfferDesc[] Find(ServiceProperty[] properties,
                                           Connection conn) {
      try {
        return conn.Offers.findServices(properties);
      }
      catch (ServiceFailure e) {
        Console.WriteLine(
          "Erro ao tentar realizar a busca por um serviço no barramento: Falha no serviço remoto. Causa:");
        Console.WriteLine(e.StackTrace);
      }
      catch (Exception e) {
        Console.WriteLine(
          "Erro inesperado ao tentar realizar a busca por um serviço no barramento:");
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
          "Falha ao tentar realizar o login por senha no barramento: a entidade já está com o login realizado. Esta falha será ignorada.");
        return true;
      }
      catch (AccessDenied) {
        Console.WriteLine(
          "Erro ao tentar realizar o login por senha no barramento: a senha fornecida não foi validada para a entidade " +
          login + ".");
      }
      catch (ServiceFailure e) {
        Console.WriteLine(
          "Erro ao tentar realizar o login por senha no barramento: Falha no serviço remoto. Causa:");
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