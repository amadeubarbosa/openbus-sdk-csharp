using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Ch.Elca.Iiop.Idl;
using Scs.Core;
using scs.core;
using tecgraf.openbus;
using tecgraf.openbus.core.v2_0.services;
using tecgraf.openbus.core.v2_0.services.access_control;
using tecgraf.openbus.core.v2_0.services.offer_registry;
using tecgraf.openbus.exceptions;

namespace hello {
  /// <summary>
  /// Servidor da demo hello.
  /// </summary>
  internal static class HelloServer {
    private static Connection _conn;
    private static ServiceOffer _offer;

    private static void Main(String[] args) {
      // Registra handler para o caso do processo ser finalizado
      AppDomain.CurrentDomain.ProcessExit += CurrentDomainProcessExit;

      // Obtém dados através dos argumentos
      string host = args[0];
      ushort port = Convert.ToUInt16(args[1]);
      string entity = args[2];
      string key = args[3];

      // Cria conexão e a define como conexão padrão tanto para entrada como saída.
      // O uso exclusivo da conexão padrão (sem uso de requester e dispatcher) só é recomendado para aplicações que criem apenas uma conexão e desejem utilizá-la em todos os casos. Para situações diferentes, consulte o manual do SDK OpenBus e/ou outras demos.
      IDictionary<string, string> props = new Dictionary<string, string>();
      ConnectionManager manager = ORBInitializer.Manager;
      _conn = manager.CreateConnection(host, port, props);
      manager.DefaultConnection = _conn;

      // Lê a chave privada de um arquivo
      byte[] privateKey = File.ReadAllBytes(key);

      // Cria o componente que conterá as facetas do servidor
      ComponentContext component =
        new DefaultComponentContext(new ComponentId("hello", 1, 0, 0, ".net"));

      // Cria a faceta Hello para o componente
      component.AddFacet("Hello", Repository.GetRepositoryID(typeof (Hello)),
                         new HelloImpl(_conn));

      // Define propriedades para a oferta de serviço a ser registrada no barramento
      IComponent ic = component.GetIComponent();
      ServiceProperty[] properties = new[] {
                                             new ServiceProperty("offer.domain",
                                                                 "OpenBus Demos")
                                           };

      // Faz o login
      if (!Login(entity, privateKey)) {
        Exit(1);
      }

      // Registra a oferta no barramento
      if (!Register(ic, properties)) {
        Exit(1);
      }

      // Registra uma callback para o caso do login ser perdido
      _conn.OnInvalidLogin = new HelloInvalidLoginCallback(
        entity, privateKey, ic, properties);

      // Mantém a thread ativa para aguardar requisições
      Console.WriteLine("Servidor no ar.");
      Thread.Sleep(Timeout.Infinite);
    }

    internal static bool Register(IComponent ic, ServiceProperty[] properties) {
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
      catch (InvalidPrivateKeyException) {
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