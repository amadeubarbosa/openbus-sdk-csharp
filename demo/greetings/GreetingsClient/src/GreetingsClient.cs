using System;
using System.Collections.Generic;
using omg.org.CORBA;
using tecgraf.openbus;
using tecgraf.openbus.core.v2_0.services;
using tecgraf.openbus.core.v2_0.services.access_control;
using tecgraf.openbus.core.v2_0.services.offer_registry;
using tecgraf.openbus.exceptions;

namespace greetings {
  /// <summary>
  /// Cliente da demo greetings.
  /// </summary>
  internal static class GreetingsClient {
    private static void Main(String[] args) {
      // Obtém dados através dos argumentos
      string host = args[0];
      short port = Convert.ToInt16(args[1]);
      string entity = args[2];
      string password = args[3];
      string greetingsEntity = args[4];

      // Cria conexão e a define como conexão padrão tanto para entrada como saída.
      // O uso exclusivo da conexão padrão (sem uso de requester e dispatcher) só é recomendado para aplicações que criem apenas uma conexão e desejem utilizá-la em todos os casos. Para situações diferentes, consulte o manual do SDK OpenBus e/ou outras demos.
      ConnectionManager manager = ORBInitializer.Manager;
      Connection conn = manager.CreateConnection(host, port);
      manager.DefaultConnection = conn;

      // Pergunta ao usuário qual língua deseja utilizar
      Console.WriteLine(
        "Digite a língua para a qual deseja obter saudações, ou enter para todas: (english, spanish, portuguese)");
      string language = Console.ReadLine();
      if (language == null) {
        Console.WriteLine("Erro ao ler a entrada do teclado.");
        Environment.Exit(1);
      }

      // Faz o login
      System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
      if (!Login(entity, encoding.GetBytes(password), conn)) {
        Exit(1);
      }

      // Faz busca utilizando propriedades geradas automaticamente e propriedades definidas pelos serviços específicos
      ServiceProperty[] properties;
      // propriedade definida pelos serviços
      ServiceProperty prop = new ServiceProperty("offer.domain", "OpenBus Demos");
      // propriedades geradas automaticamente
      ServiceProperty autoProp1 = new ServiceProperty("openbus.offer.entity",
                                                      greetingsEntity);
      if (!language.Equals("")) {
        ServiceProperty autoProp2 = new ServiceProperty(
          "openbus.component.name", language.ToLower());
        properties = new[] {prop, autoProp1, autoProp2};
      }
      else {
        Console.WriteLine(
          "Nenhuma língua especificada. Todos os serviços greetings que forem encontrados serão utilizados.");
        properties = new[] {prop, autoProp1};
      }

      ServiceOfferDesc[] offers = Find(properties, conn);

      // analiza as ofertas encontradas
      IEnumerable<Greetings> greetings = GetGreetings(offers);
      if (greetings == null) {
        Exit(1);
      }
      else {
        // utiliza o serviço
        foreach (Greetings greeting in greetings) {
          Console.WriteLine((string)greeting.sayGreetings());
        }
      }

      conn.Logout();
      Console.WriteLine("Fim.");
      Console.ReadLine();
    }

    private static IEnumerable<Greetings> GetGreetings(
      ICollection<ServiceOfferDesc> offers) {
      if (offers.Count < 1) {
        Console.WriteLine(
          "O serviço Greetings na língua especificada não se encontra no barramento.");
        return null;
      }

      if (offers.Count > 1) {
        Console.WriteLine(
          "Existe mais de um serviço Greetings no barramento com a língua especificada. Tentaremos encontrar uma funcional.");
      }
      IList<Greetings> ret = new List<Greetings>();
      foreach (ServiceOfferDesc serviceOfferDesc in offers) {
        Console.WriteLine("Testando uma das ofertas recebidas...");
        try {
          int hours = DateTime.Now.TimeOfDay.Hours;
          MarshalByRefObject greetObj = hours < 12
                                          ? serviceOfferDesc.service_ref.
                                              getFacetByName("GoodMorning")
                                          : serviceOfferDesc.service_ref.
                                              getFacetByName(hours >= 18
                                                               ? "GoodNight"
                                                               : "GoodAfternoon");
          if (greetObj == null) {
            Console.WriteLine(
              "Não foi possível encontrar uma faceta Greetings na oferta.");
            continue;
          }
          Greetings greetings = greetObj as Greetings;
          if (greetings == null) {
            Console.WriteLine(
              "Faceta encontrada na oferta não implementa Greetings.");
            continue;
          }
          Console.WriteLine(
            "Foi encontrada uma oferta com um serviço funcional.");
          ret.Add(greetings);
        }
        catch (TRANSIENT) {
          Console.WriteLine(
            "A oferta é de um cliente inativo. Tentando a próxima.");
        }
      }
      Console.WriteLine(
        String.Format("Foram encontradas {0} ofertas com serviços funcionais.",
                      ret.Count));
      return ret;
    }

    private static ServiceOfferDesc[] Find(ServiceProperty[] properties,
                                           Connection conn) {
      try {
        return conn.Offers.findServices(properties);
      }
      catch (ServiceFailure e) {
        Console.WriteLine(
          "Erro ao tentar realizar a busca por um serviço no barramento: Falha no serviço remoto. Causa:");
        Console.WriteLine(e);
      }
      catch (Exception e) {
        Console.WriteLine(
          "Erro inesperado ao tentar realizar a busca por um serviço no barramento:");
        Console.WriteLine(e);
      }
      return null;
    }

    private static bool Login(string login, byte[] password, Connection conn) {
      try {
        conn.LoginByPassword(login, password);
        return true;
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
        Console.WriteLine(e);
      }
      catch (Exception e) {
        Console.WriteLine(
          "Erro inesperado ao tentar realizar o login por senha no barramento:");
        Console.WriteLine(e);
      }
      return false;
    }

    private static void Exit(int code) {
      Console.WriteLine("Pressione qualquer tecla para sair.");
      Console.ReadLine();
      Environment.Exit(code);
    }
  }
}