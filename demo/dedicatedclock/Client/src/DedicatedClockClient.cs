using System;
using System.Collections.Generic;
using System.Threading;
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
    private static Connection _conn;

    private static void Main(String[] args) {
      // Obtém dados através dos argumentos
      string host = args[0];
      short port = Convert.ToInt16(args[1]);
      string entity = args[2];
      byte[] password = new System.Text.UTF8Encoding().GetBytes(args[3]);
      string serverEntity = args[4];
      int waitTime = Convert.ToInt32(args[5]);
      int totalWaitTime = Convert.ToInt32(args[6]);

      DateTime max = DateTime.Now.AddMilliseconds(totalWaitTime);
      bool ok = false;
      bool firstTime = true;

      while (true) {
        // Se não for a primeira vez, espera
        if (firstTime) {
          firstTime = false;
        }
        else {
          Console.WriteLine(String.Format("Aguardando {0} milisegundos.",
                                          waitTime));
          Thread.Sleep(waitTime);
          if (DateTime.Now > max) {
            break;
          }
        }

        // Tenta conectar
        if (_conn == null) {
          if (!Connect(host, port)) {
            continue;
          }
        }

        // Faz o login
        if ((_conn.Login == null) && (!Login(entity, password))) {
          continue;
        }

        // Faz busca utilizando propriedades geradas automaticamente e propriedades definidas pelo serviço específico
        // propriedade gerada automaticamente
        ServiceProperty autoProp = new ServiceProperty("openbus.offer.entity",
                                                       serverEntity);
        // propriedade definida pelo serviço clock
        ServiceProperty prop = new ServiceProperty("offer.domain",
                                                   "OpenBus Demos");
        ServiceProperty[] properties = new[] {prop, autoProp};
        ServiceOfferDesc[] offers = Find(properties);
        if (offers == null) {
          continue;
        }

        // analiza as ofertas encontradas
        Clock clock = GetClock(offers);
        if (clock == null) {
          continue;
        }

        // utiliza o serviço
        long ticks = clock.getTimeInTicks();
        DateTime serverTime = new DateTime(ticks);
        Console.WriteLine(String.Format("Hora do servidor: {0:HH:mm:ss}",
                                        serverTime));
        ok = true;
        break;
      }
      Console.WriteLine(ok
                          ? "Fim."
                          : "Não foi possível realizar o login ou encontrar o servidor.");
      Console.WriteLine("Pressione qualquer tecla para finalizar.");
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

    private static ServiceOfferDesc[] Find(ServiceProperty[] properties) {
      try {
        return _conn.Offers.findServices(properties);
      }
      catch (ServiceFailure e) {
        Console.WriteLine(
          "Erro ao tentar realizar a busca por um serviço no barramento: Falha no serviço remoto. Causa:");
        Console.WriteLine(e);
      }
      catch (NO_PERMISSION e) {
        if (e.Minor.Equals(NoLoginCode.ConstVal)) {
          // o login foi perdido, precisa tentar fazer o login novamente
          return null;
        }
      }
      catch (Exception e) {
        Console.WriteLine(
          "Erro inesperado ao tentar realizar a busca por um serviço no barramento:");
        Console.WriteLine(e);
        _conn = null;
      }
      return null;
    }

    private static bool Login(string login, byte[] password) {
      try {
        _conn.LoginByPassword(login, password);
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
      catch (TRANSIENT) {
        Console.WriteLine(
          "O barramento não está acessível para realizar o login.");
        _conn = null;
      }
      catch (Exception e) {
        Console.WriteLine(
          "Erro inesperado ao tentar realizar o login por senha no barramento:");
        Console.WriteLine(e);
        _conn = null;
      }
      return false;
    }

    private static bool Connect(string host, short port) {
      // Cria conexão e a define como conexão padrão tanto para entrada como saída.
      // O uso exclusivo da conexão padrão (sem uso de requester e dispatcher) só é recomendado para aplicações que criem apenas uma conexão e desejem utilizá-la em todos os casos. Para situações diferentes, consulte o manual do SDK OpenBus e/ou outras demos.
      ConnectionManager manager = ORBInitializer.Manager;
      try {
        _conn = manager.CreateConnection(host, port);
        manager.DefaultConnection = _conn;
        return true;
      }
      catch (TRANSIENT) {
        Console.WriteLine("O barramento não está acessível.");
      }
      catch (Exception e) {
        Console.WriteLine("Erro inesperado ao tentar conectar ao barramento:");
        Console.WriteLine(e);
      }
      _conn = null;
      return false;
    }
  }
}