using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Ch.Elca.Iiop.Idl;
using Scs.Core;
using omg.org.CORBA;
using scs.core;
using tecgraf.openbus;
using tecgraf.openbus.core.v2_0.services;
using tecgraf.openbus.core.v2_0.services.access_control;
using tecgraf.openbus.core.v2_0.services.offer_registry;
using tecgraf.openbus.exceptions;

namespace chainvalidation {
  /// <summary>
  /// Servidor Secret�ria da demo ChainValidation.
  /// </summary>
  internal static class SecretaryServer {
    private static Connection _conn;
    private static ServiceOffer _offer;

    private static void Main(String[] args) {
      // Registra handler para o caso do processo ser finalizado
      AppDomain.CurrentDomain.ProcessExit += CurrentDomainProcessExit;

      // Obt�m dados atrav�s dos argumentos
      string host = args[0];
      ushort port = Convert.ToUInt16(args[1]);
      string entity = args[2];
      string key = args[3];
      string executiveEntity = args[4];

      // Cria conex�o e a define como conex�o padr�o tanto para entrada como sa�da.
      // O uso exclusivo da conex�o padr�o (sem uso de requester e dispatcher) s� � recomendado para aplica��es que criem apenas uma conex�o e desejem utiliz�-la em todos os casos. Para situa��es diferentes, consulte o manual do SDK OpenBus e/ou outras demos.
      IDictionary<string, string> props = new Dictionary<string, string>();
      ConnectionManager manager = ORBInitializer.Manager;
      _conn = manager.CreateConnection(host, port, props);
      manager.DefaultConnection = _conn;

      // L� a chave privada de um arquivo
      byte[] privateKey = File.ReadAllBytes(key);

      // Faz o login
      if (!Login(entity, privateKey)) {
        Exit(1);
      }

      // Registra a oferta no barramento
      if (!Register(executiveEntity)) {
        Exit(1);
      }

      // Registra uma callback para o caso do login ser perdido
      _conn.OnInvalidLogin = new ChainValidationInvalidLoginCallback(entity,
                                                                     privateKey,
                                                                     executiveEntity);

      // Mant�m a thread ativa para aguardar requisi��es
      Console.WriteLine("Servidor no ar.");
      Thread.Sleep(Timeout.Infinite);
    }

    private static IComponent CreateComponent(Message executive) {
      // Cria o componente que conter� as facetas do servidor
      ComponentContext component =
        new DefaultComponentContext(new ComponentId("secretary", 1, 0, 0, ".net"));

      // Cria as facetas para o componente
      component.AddFacet("Message", Repository.GetRepositoryID(typeof (Message)),
                         new MessageImpl(_conn));
      component.AddFacet("Meeting", Repository.GetRepositoryID(typeof (Meeting)),
                         new MeetingImpl(_conn, executive));

      return component.GetIComponent();
    }

    private static ServiceOfferDesc[] Find(ServiceProperty[] properties) {
      try {
        return _conn.Offers.findServices(properties);
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

    private static Message GetExecutive(ICollection<ServiceOfferDesc> offers) {
      if (offers.Count < 1) {
        Console.WriteLine("O servi�o executive n�o se encontra no barramento.");
        return null;
      }

      if (offers.Count > 1) {
        Console.WriteLine(
          "Existe mais de um servi�o executive no barramento. Tentaremos encontrar um funcional.");
      }
      foreach (ServiceOfferDesc serviceOfferDesc in offers) {
        Console.WriteLine("Testando uma das ofertas recebidas...");
        try {
          MarshalByRefObject executiveObj =
            serviceOfferDesc.service_ref.getFacet("IDL:Message:1.0");
          if (executiveObj == null) {
            Console.WriteLine(
              "N�o foi poss�vel encontrar uma faceta Message na oferta.");
            continue;
          }
          Message msg = executiveObj as Message;
          if (msg == null) {
            Console.WriteLine(
              "Faceta encontrada na oferta n�o implementa Message.");
            continue;
          }
          Console.WriteLine(
            "Foi encontrada uma oferta com um servi�o funcional.");
          return msg;
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

    internal static bool Register(string executiveEntity) {
      //busca executivo
      ServiceProperty autoProp1 = new ServiceProperty("openbus.offer.entity",
                                                      executiveEntity);
      ServiceProperty autoProp2 = new ServiceProperty("openbus.component.name",
                                                      "executive");
      // propriedade definida pelo servi�o
      ServiceProperty prop = new ServiceProperty("offer.domain", "OpenBus Demos");
      ServiceProperty[] executiveProps = new[] {prop, autoProp1, autoProp2};
      ServiceOfferDesc[] offers = Find(executiveProps);

      // analiza as ofertas encontradas
      Message executive = GetExecutive(offers);
      if (executive == null) {
        Exit(1);
      }

      IComponent ic = CreateComponent(executive);
      // Define propriedades para a oferta de servi�o a ser registrada no barramento
      ServiceProperty[] properties = new[] {
                                             new ServiceProperty("offer.domain",
                                                                 "OpenBus Demos")
                                           };

      try {
        _offer = _conn.Offers.registerService(ic, properties);
        return true;
      }
      catch (InvalidService) {
        Console.WriteLine(
          "Erro ao tentar registrar a oferta no barramento: o IComponent fornecido n�o � v�lido, por n�o apresentar facetas padr�o definidas pelo modelo de componetes SCS.");
      }
      catch (InvalidProperties) {
        Console.WriteLine(
          "Erro ao tentar registrar a oferta no barramento: A lista de propriedades fornecida inclui propriedades inv�lidas, tais como propriedades com nomes reservados (cujos nomes come�am com 'openbus.').");
      }
      catch (UnauthorizedFacets) {
        Console.WriteLine(
          "Erro ao tentar registrar a oferta no barramento: O componente que implementa o servi�o apresenta facetas com interfaces que n�o est�o autorizadas para a entidade realizando o registro da oferta de servi�o.");
      }
      catch (Exception e) {
        Console.WriteLine(
          "Erro inesperado ao tentar registrar a oferta no barramento:");
        Console.WriteLine(e);
      }
      return false;
    }

    internal static bool Login(string entity, byte[] privateKey) {
      try {
        _conn.LoginByCertificate(entity, privateKey);
        return true;
      }
      catch (AlreadyLoggedInException) {
        Console.WriteLine(
          "Falha ao tentar realizar o login por certificado no barramento: a entidade j� est� com o login realizado. Esta falha ser� ignorada.");
        return true;
      }
      catch (InvalidPrivateKeyException) {
        Console.WriteLine(
          "Erro ao tentar realizar o login por certificado no barramento: a chave privada est� corrompida ou em um formato errado.");
      }
      catch (AccessDenied) {
        Console.WriteLine(
          "Erro ao tentar realizar o login por certificado no barramento: a chave privada fornecida n�o � a esperada.");
      }
      catch (MissingCertificate) {
        Console.WriteLine(
          "Erro ao tentar realizar o login por certificado no barramento: o barramento n�o tem certificado registrado para a entidade " +
          entity);
      }
      catch (ServiceFailure e) {
        Console.WriteLine(
          "Erro ao tentar realizar o login por certificado no barramento: Falha no servi�o remoto. Causa:");
        Console.WriteLine(e);
      }
      catch (Exception e) {
        Console.WriteLine(
          "Erro inesperado ao tentar realizar o login por certificado no barramento:");
        Console.WriteLine(e);
      }
      return false;
    }

    private static void CurrentDomainProcessExit(object sender, EventArgs e) {
      if (_offer == null) {
        return;
      }
      try {
        Console.WriteLine(
          "Removendo oferta do barramento antes de terminar o processo...");
        _offer.remove();
        Console.WriteLine("Oferta removida do barramento.");
      }
      catch (UnauthorizedOperation) {
        Console.WriteLine(
          "Erro ao tentar remover a oferta do barramento: opera��o n�o autorizada. O login utilizado para remover a oferta n�o � o mesmo que a registrou e n�o � um administrador do barramento.");
      }
      catch (ServiceFailure exc) {
        Console.WriteLine(
          "Erro ao tentar remover a oferta do barramento: erro no servi�o remoto. Causa:");
        Console.WriteLine(exc);
      }
      catch (Exception exc) {
        Console.WriteLine(
          "Erro inesperado ao tentar remover a oferta do barramento:");
        Console.WriteLine(exc);
      }
      if (_conn != null) {
        _conn.Logout();
      }
    }

    private static void Exit(int code) {
      _conn.Logout();
      Console.WriteLine("Pressione qualquer tecla para sair.");
      Console.ReadLine();
      Environment.Exit(code);
    }
  }
}