using System;
using omg.org.CORBA;
using tecgraf.openbus;
using tecgraf.openbus.core.v2_0.services;
using tecgraf.openbus.core.v2_0.services.access_control;
using tecgraf.openbus.core.v2_0.services.offer_registry;
using tecgraf.openbus.exceptions;

namespace chainvalidation {
  /// <summary>
  /// Cliente da demo ChainValidation.
  /// </summary>
  internal static class ChainValidationClient {
    private static void Main(String[] args) {
      string host = args[0];
      short port = Convert.ToInt16(args[1]);
      string entity = args[2];
      string password = args[3];

      // Cria conexão e a define como conexão padrão tanto para entrada como saída.
      // O uso exclusivo da conexão padrão (sem uso de requester e dispatcher) só é recomendado para aplicações que criem apenas uma conexão e desejem utilizá-la em todos os casos. Para situações diferentes, consulte o manual do SDK OpenBus e/ou outras demos.
      ConnectionManager manager = ORBInitializer.Manager;
      Connection conn = manager.CreateConnection(host, port);
      manager.DefaultConnection = conn;

      // Faz o login
      System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
      if (!Login(entity, encoding.GetBytes(password), conn)) {
        Console.ReadLine();
        Environment.Exit(1);
      }

      // analiza as ofertas encontradas
      Meeting secretaryMeeting;
      Meeting dummyMeeting;
      Message executiveMsg = GetService(conn, "executive", out dummyMeeting);
      Message secretaryMessage = GetService(conn, "secretary", out secretaryMeeting);
      if ((executiveMsg == null) || (secretaryMessage == null) || (secretaryMeeting == null)) {
        Console.ReadLine();
        Environment.Exit(1);
      }

      // utiliza o serviço
      bool failed = false;
      try {
        executiveMsg.sendMessage("Olá!");
      }
      catch (Unavailable) {
        failed = true;
      }
      if (!failed) {
        Console.WriteLine("Executivo aceitou uma mensagem diretamente, o que não deveria ter acontecido.");
        Console.ReadLine();
        Environment.Exit(1);
      }
      secretaryMessage.sendMessage("Olá, eu gostaria de agendar uma reunião com seu chefe.");
      int hour = secretaryMeeting.bookMeeting();
      Console.WriteLine(String.Format("Uma reunião foi agendada para as {0}h.", hour));

      Console.WriteLine("Fim.");
      Console.ReadLine();
    }

    private static Message GetService(Connection conn, string name, out Meeting meeting) {
      // Faz busca utilizando propriedades geradas automaticamente e propriedades definidas pelo serviço específico
      // propriedades geradas automaticamente
      ServiceProperty autoProp = new ServiceProperty("openbus.component.name", name);
      // propriedade definida pelo serviço
      ServiceProperty prop = new ServiceProperty("offer.domain", "OpenBus Demos");
      ServiceProperty[] properties = new[] { prop, autoProp };
      ServiceOfferDesc[] offers = Find(properties, conn);
      if (offers == null) {
        Console.ReadLine();
        Environment.Exit(1);
      }

      if (offers.Length < 1) {
        Console.WriteLine(String.Format("O componente {0} não se encontra no barramento.", name));
        meeting = null;
        return null;
      }

      if (offers.Length > 1) {
        Console.WriteLine(String.Format("Existe mais de um componente {0} no barramento. Tentaremos encontrar um funcional.", name));
      }
      foreach (ServiceOfferDesc serviceOfferDesc in offers) {
        Console.WriteLine("Testando uma das ofertas recebidas...");
        try {
          MarshalByRefObject msgObj =
            serviceOfferDesc.service_ref.getFacet("IDL:Message:1.0");
          if (msgObj == null) {
            Console.WriteLine(
              "Não foi possível encontrar uma faceta Message na oferta.");
            continue;
          }
          Message message = msgObj as Message;
          if (message == null) {
            Console.WriteLine(
              "Faceta encontrada na oferta não implementa Message.");
            continue;
          }
          Console.WriteLine(
            "Foi encontrada uma oferta com um serviço Message funcional.");
          if (name.Equals("executive")) {
            meeting = null;
          }
          else {
            MarshalByRefObject mtnObj =
              serviceOfferDesc.service_ref.getFacet("IDL:Meeting:1.0");
            if (mtnObj == null) {
              Console.WriteLine(
                "Não foi possível encontrar uma faceta Meeting na oferta.");
              continue;
            }
            meeting = mtnObj as Meeting;
            if (meeting == null) {
              Console.WriteLine(
                "Faceta encontrada na oferta não implementa Meeting.");
              continue;
            }
            Console.WriteLine(
              "Foi encontrada uma oferta com um serviço Meeting funcional.");
            return message;
          }
          return message;
        }
        catch (TRANSIENT) {
          Console.WriteLine(
            "A oferta é de um cliente inativo. Tentando a próxima.");
        }
      }
      Console.WriteLine(
        "Não foi encontrada uma oferta com um serviço funcional.");
      meeting = null;
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