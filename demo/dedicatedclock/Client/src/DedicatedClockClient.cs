using System;
using System.Collections.Generic;
using omg.org.CORBA;
using tecgraf.openbus;
using tecgraf.openbus.core.v2_00.services;
using tecgraf.openbus.core.v2_00.services.access_control;
using tecgraf.openbus.core.v2_00.services.offer_registry;
using tecgraf.openbus.exceptions;

namespace Client {
  /// <summary>
  /// Cliente da demo Dedicated Clock.
  /// </summary>
  internal static class DedicatedClockClient {
    private static void Main(String[] args) {
      // Obtém dados através dos argumentos
      string host = args[0];
      short port = Convert.ToInt16(args[1]);
      string entity = args[2];
      string password = args[3];
      string serverEntity = args[4];

      // Cria conexão e a define como conexão padrão tanto para entrada como saída.
      // O uso exclusivo da conexão padrão (sem uso de requester e dispatcher) só é recomendado para aplicações que criem apenas uma conexão e desejem utilizá-la em todos os casos. Para situações diferentes, consulte o manual do SDK OpenBus e/ou outras demos.
      ConnectionManager manager = ORBInitializer.Manager;
      //TODO aqui deve tentar conectar e logar no barramento durante um tempo. Se não conseguir, falhar com mensagem de indisponibilidade.
      Connection conn = manager.CreateConnection(host, port);
      manager.DefaultConnection = conn;

      // Faz o login
      System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
      if (!Login(entity, encoding.GetBytes(password), conn)) {
        Console.ReadLine();
        Environment.Exit(1);
      }

      // Faz busca utilizando propriedades geradas automaticamente e propriedades definidas pelo serviço específico
      // propriedade gerada automaticamente
      ServiceProperty autoProp = new ServiceProperty("openbus.offer.entity",
                                                     serverEntity);
      // propriedade definida pelo serviço clock
      ServiceProperty prop = new ServiceProperty("offer.domain", "OpenBus Demos");
      ServiceProperty[] properties = new[] {prop, autoProp};
      ServiceOfferDesc[] offers = Find(properties, conn);
      if (offers == null) {
        Console.ReadLine();
        Environment.Exit(1);
      }

      // analiza as ofertas encontradas
      Clock clock = GetClock(offers);
      if (clock == null) {
        Console.ReadLine();
        Environment.Exit(1);
      }

      // utiliza o serviço
      long ticks = clock.getTimeInTicks();
      DateTime serverTime = new DateTime(ticks);
      Console.WriteLine(String.Format("Hora do servidor: {0}:{1}:{2}",
                                      serverTime.Hour,
                                      serverTime.Minute, serverTime.Second));

      Console.WriteLine("Fim.");
      Console.ReadLine();
    }

    private static Clock GetClock(ICollection<ServiceOfferDesc> offers) {
      if (offers.Count < 1) {
        Console.WriteLine("O serviço Clock não se encontra no barramento.");
        return null;
      }

      if (offers.Count > 1) {
        Console.WriteLine(
          "Existe mais de um serviço Clock no barramento. Tentaremos encontrar uma funcional.");
      }
      foreach (ServiceOfferDesc serviceOfferDesc in offers) {
        Console.WriteLine("Testando uma das ofertas recebidas...");
        try {
          MarshalByRefObject clockObj =
            serviceOfferDesc.service_ref.getFacet(
              "IDL:Clock:1.0");
          if (clockObj == null) {
            Console.WriteLine(
              "Não foi possível encontrar uma faceta Clock na oferta.");
            continue;
          }
          Clock clock = clockObj as Clock;
          if (clock == null) {
            Console.WriteLine(
              "Faceta encontrada na oferta não implementa Clock.");
            continue;
          }
          Console.WriteLine(
            "Foi encontrada uma oferta com um serviço funcional.");
          return clock;
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
  }
}