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

      // Cria conex�o e a define como conex�o padr�o tanto para entrada como sa�da.
      // O uso exclusivo da conex�o padr�o (sem uso de requester e dispatcher) s� � recomendado para aplica��es que criem apenas uma conex�o e desejem utiliz�-la em todos os casos. Para situa��es diferentes, consulte o manual do SDK OpenBus e/ou outras demos.
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

      // utiliza o servi�o
      bool failed = false;
      try {
        executiveMsg.sendMessage("Ol�!");
      }
      catch (Unavailable) {
        failed = true;
      }
      if (!failed) {
        Console.WriteLine("Executivo aceitou uma mensagem diretamente, o que n�o deveria ter acontecido.");
        Console.ReadLine();
        Environment.Exit(1);
      }
      secretaryMessage.sendMessage("Ol�, eu gostaria de agendar uma reuni�o com seu chefe.");
      int hour = secretaryMeeting.bookMeeting();
      Console.WriteLine(String.Format("Uma reuni�o foi agendada para as {0}h.", hour));

      Console.WriteLine("Fim.");
      Console.ReadLine();
    }

    private static Message GetService(Connection conn, string name, out Meeting meeting) {
      // Faz busca utilizando propriedades geradas automaticamente e propriedades definidas pelo servi�o espec�fico
      // propriedades geradas automaticamente
      ServiceProperty autoProp = new ServiceProperty("openbus.component.name", name);
      // propriedade definida pelo servi�o
      ServiceProperty prop = new ServiceProperty("offer.domain", "OpenBus Demos");
      ServiceProperty[] properties = new[] { prop, autoProp };
      ServiceOfferDesc[] offers = Find(properties, conn);
      if (offers == null) {
        Console.ReadLine();
        Environment.Exit(1);
      }

      if (offers.Length < 1) {
        Console.WriteLine(String.Format("O componente {0} n�o se encontra no barramento.", name));
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
              "N�o foi poss�vel encontrar uma faceta Message na oferta.");
            continue;
          }
          Message message = msgObj as Message;
          if (message == null) {
            Console.WriteLine(
              "Faceta encontrada na oferta n�o implementa Message.");
            continue;
          }
          Console.WriteLine(
            "Foi encontrada uma oferta com um servi�o Message funcional.");
          if (name.Equals("executive")) {
            meeting = null;
          }
          else {
            MarshalByRefObject mtnObj =
              serviceOfferDesc.service_ref.getFacet("IDL:Meeting:1.0");
            if (mtnObj == null) {
              Console.WriteLine(
                "N�o foi poss�vel encontrar uma faceta Meeting na oferta.");
              continue;
            }
            meeting = mtnObj as Meeting;
            if (meeting == null) {
              Console.WriteLine(
                "Faceta encontrada na oferta n�o implementa Meeting.");
              continue;
            }
            Console.WriteLine(
              "Foi encontrada uma oferta com um servi�o Meeting funcional.");
            return message;
          }
          return message;
        }
        catch (TRANSIENT) {
          Console.WriteLine(
            "A oferta � de um cliente inativo. Tentando a pr�xima.");
        }
      }
      Console.WriteLine(
        "N�o foi encontrada uma oferta com um servi�o funcional.");
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
          "Erro ao tentar realizar a busca por um servi�o no barramento: Falha no servi�o remoto. Causa:");
        Console.WriteLine(e);
      }
      catch (Exception e) {
        Console.WriteLine(
          "Erro inesperado ao tentar realizar a busca por um servi�o no barramento:");
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