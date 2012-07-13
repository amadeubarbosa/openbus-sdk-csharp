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

namespace audit {
  /// <summary>
  /// Proxy da demo Audit.
  /// </summary>
  internal static class AuditProxy {
    private static Connection _conn;
    private static ServiceOffer _offer;

    private static void Main(String[] args) {
      // Registra handler para o caso do processo ser finalizado
      AppDomain.CurrentDomain.ProcessExit += CurrentDomainProcessExit;

      // Obtém dados através dos argumentos
      string host = args[0];
      short port = Convert.ToInt16(args[1]);
      string entity = args[2];
      string key = args[3];
      string serverEntity = args[4];

      // Cria conexão e a define como conexão padrão tanto para entrada como saída.
      // O uso exclusivo da conexão padrão (sem uso de requester e dispatcher) só é recomendado para aplicações que criem apenas uma conexão e desejem utilizá-la em todos os casos. Para situações diferentes, consulte o manual do SDK OpenBus e/ou outras demos.
      ConnectionManager manager = ORBInitializer.Manager;
      _conn = manager.CreateConnection(host, port);
      manager.DefaultConnection = _conn;

      // Lê a chave privada de um arquivo
      byte[] privateKey = File.ReadAllBytes(key);

      // Faz o login
      if (!Login(entity, privateKey)) {
        Exit(1);
      }

      // Registra a oferta no barramento
      if (!Register(serverEntity)) {
        Exit(1);
      }

      // Registra uma callback para o caso do login ser perdido
      _conn.OnInvalidLogin = new ProxyInvalidLogin(entity, privateKey,
                                                   serverEntity);

      // Mantém a thread ativa para aguardar requisições
      Console.WriteLine("Servidor no ar.");
      Thread.Sleep(Timeout.Infinite);
    }

    private static IComponent CreateComponent(Hello server) {
      // Cria o componente que conterá as facetas do servidor
      ComponentContext component =
        new DefaultComponentContext(new ComponentId("audit proxy", 1, 0, 0,
                                                    ".net"));

      // Cria as facetas para o componente
      component.AddFacet("Hello", Repository.GetRepositoryID(typeof (Hello)),
                         new HelloImpl(_conn, server));

      return component.GetIComponent();
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
      catch (Exception e) {
        Console.WriteLine(
          "Erro inesperado ao tentar realizar a busca por um serviço no barramento:");
        Console.WriteLine(e);
      }
      return null;
    }

    private static Hello GetServer(ICollection<ServiceOfferDesc> offers) {
      if (offers.Count < 1) {
        Console.WriteLine("O servidor não se encontra no barramento.");
        return null;
      }

      if (offers.Count > 1) {
        Console.WriteLine(
          "Existe mais de um servidor no barramento. Tentaremos encontrar um funcional.");
      }
      foreach (ServiceOfferDesc serviceOfferDesc in offers) {
        Console.WriteLine("Testando uma das ofertas recebidas...");
        try {
          MarshalByRefObject serverObj =
            serviceOfferDesc.service_ref.getFacet("IDL:Hello:1.0");
          if (serverObj == null) {
            Console.WriteLine(
              "Não foi possível encontrar uma faceta Hello na oferta.");
            continue;
          }
          Hello msg = serverObj as Hello;
          if (msg == null) {
            Console.WriteLine(
              "Faceta encontrada na oferta não implementa Hello.");
            continue;
          }
          Console.WriteLine(
            "Foi encontrada uma oferta com um serviço funcional.");
          return msg;
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

    internal static bool Register(string serverEntity) {
      //busca servidor
      ServiceProperty autoProp1 = new ServiceProperty("openbus.offer.entity",
                                                      serverEntity);
      ServiceProperty autoProp2 = new ServiceProperty("openbus.component.name",
                                                      "audit server");
      // propriedade definida pelo serviço
      ServiceProperty prop = new ServiceProperty("offer.domain", "OpenBus Demos");
      ServiceProperty[] serverProps = new[] {prop, autoProp1, autoProp2};
      ServiceOfferDesc[] offers = Find(serverProps);

      // analiza as ofertas encontradas
      Hello server = GetServer(offers);
      if (server == null) {
        Exit(1);
      }

      IComponent ic = CreateComponent(server);
      // Define propriedades para a oferta de serviço a ser registrada no barramento
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
          "Erro ao tentar registrar a oferta no barramento: o IComponent fornecido não é válido, por não apresentar facetas padrão definidas pelo modelo de componetes SCS.");
      }
      catch (InvalidProperties) {
        Console.WriteLine(
          "Erro ao tentar registrar a oferta no barramento: A lista de propriedades fornecida inclui propriedades inválidas, tais como propriedades com nomes reservados (cujos nomes começam com 'openbus.').");
      }
      catch (UnauthorizedFacets) {
        Console.WriteLine(
          "Erro ao tentar registrar a oferta no barramento: O componente que implementa o serviço apresenta facetas com interfaces que não estão autorizadas para a entidade realizando o registro da oferta de serviço.");
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
          "Falha ao tentar realizar o login por certificado no barramento: a entidade já está com o login realizado. Esta falha será ignorada.");
        return true;
      }
      catch (CorruptedPrivateKeyException) {
        Console.WriteLine(
          "Erro ao tentar realizar o login por certificado no barramento: a chave privada está corrompida ou em um formato errado.");
      }
      catch (WrongPrivateKeyException) {
        Console.WriteLine(
          "Erro ao tentar realizar o login por certificado no barramento: a chave privada fornecida não é a esperada.");
      }
      catch (MissingCertificate) {
        Console.WriteLine(
          "Erro ao tentar realizar o login por certificado no barramento: o barramento não tem certificado registrado para a entidade " +
          entity);
      }
      catch (ServiceFailure e) {
        Console.WriteLine(
          "Erro ao tentar realizar o login por certificado no barramento: Falha no serviço remoto. Causa:");
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
          "Erro ao tentar remover a oferta do barramento: operação não autorizada. O login utilizado para remover a oferta não é o mesmo que a registrou e não é um administrador do barramento.");
      }
      catch (ServiceFailure exc) {
        Console.WriteLine(
          "Erro ao tentar remover a oferta do barramento: erro no serviço remoto. Causa:");
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