using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Ch.Elca.Iiop.Idl;
using Scs.Core;
using scs.core;
using tecgraf.openbus;
using tecgraf.openbus.core.v2_00.services;
using tecgraf.openbus.core.v2_00.services.access_control;
using tecgraf.openbus.core.v2_00.services.offer_registry;
using tecgraf.openbus.exceptions;

namespace multiplexing {
  /// <summary>
  /// Servidor da demo multiplexing.
  /// </summary>
  internal static class MultiplexingServer {
    private static readonly IDictionary<Connection, ServiceOffer> Offers =
      new Dictionary<Connection, ServiceOffer>();

    private static void Main(String[] args) {
      // Registra handler para o caso do processo ser finalizado
      AppDomain.CurrentDomain.ProcessExit += CurrentDomainProcessExit;

      // Obtém dados através dos argumentos
      string host = args[0];
      short port = Convert.ToInt16(args[1]);
      string entity = args[2];
      string key = args[3];

      // Cria 3 conexões com o mesmo barramento, uma para cada componente.
      ConnectionManager manager = ORBInitializer.Manager;
      Connection conn = manager.CreateConnection(host, port);
      Connection conn2 = manager.CreateConnection(host, port);
      Connection conn3 = manager.CreateConnection(host, port);

      // Lê a chave privada de um arquivo
      byte[] privateKey = File.ReadAllBytes(key);

      // Faz o login das três conexões
      if (!Login(conn, entity, privateKey)) {
        Console.ReadLine();
        Environment.Exit(1);
      }
      if (!Login(conn2, entity, privateKey)) {
        Console.ReadLine();
        Environment.Exit(1);
      }
      if (!Login(conn3, entity, privateKey)) {
        Console.ReadLine();
        Environment.Exit(1);
      }

      // Escolhe uma conexão para atender requisições
      manager.SetDispatcher(conn2);
      // Se ocorrer um logout, o dispatcher será removido, portanto colocamos a conn2 também como default
      manager.DefaultConnection = conn2;

      // Cria o componente que responde em inglês
      ComponentContext english =
        new DefaultComponentContext(new ComponentId("english", 1, 0, 0, ".net"));
      english.AddFacet("GoodMorning",
                       Repository.GetRepositoryID(typeof (Greetings)),
                       new GreetingsImpl(conn2,
                                         GreetingsImpl.Languages.English,
                                         GreetingsImpl.Period.Morning));
      english.AddFacet("GoodAfternoon",
                       Repository.GetRepositoryID(typeof (Greetings)),
                       new GreetingsImpl(conn2,
                                         GreetingsImpl.Languages.English,
                                         GreetingsImpl.Period.Afternoon));
      english.AddFacet("GoodNight",
                       Repository.GetRepositoryID(typeof (Greetings)),
                       new GreetingsImpl(conn2,
                                         GreetingsImpl.Languages.English,
                                         GreetingsImpl.Period.Night));

      // Cria o componente que responde em espanhol
      ComponentContext spanish =
        new DefaultComponentContext(new ComponentId("spanish", 1, 0, 0, ".net"));
      spanish.AddFacet("GoodMorning",
                       Repository.GetRepositoryID(typeof (Greetings)),
                       new GreetingsImpl(conn2,
                                         GreetingsImpl.Languages.Spanish,
                                         GreetingsImpl.Period.Morning));
      spanish.AddFacet("GoodAfternoon",
                       Repository.GetRepositoryID(typeof (Greetings)),
                       new GreetingsImpl(conn2,
                                         GreetingsImpl.Languages.Spanish,
                                         GreetingsImpl.Period.Afternoon));
      spanish.AddFacet("GoodNight",
                       Repository.GetRepositoryID(typeof (Greetings)),
                       new GreetingsImpl(conn2,
                                         GreetingsImpl.Languages.Spanish,
                                         GreetingsImpl.Period.Night));

      // Cria o componente que responde em português
      ComponentContext portuguese =
        new DefaultComponentContext(new ComponentId("portuguese", 1, 0, 0,
                                                    ".net"));
      portuguese.AddFacet("GoodMorning",
                          Repository.GetRepositoryID(typeof (Greetings)),
                          new GreetingsImpl(conn2,
                                            GreetingsImpl.Languages.Portuguese,
                                            GreetingsImpl.Period.Morning));
      portuguese.AddFacet("GoodAfternoon",
                          Repository.GetRepositoryID(typeof (Greetings)),
                          new GreetingsImpl(conn2,
                                            GreetingsImpl.Languages.Portuguese,
                                            GreetingsImpl.Period.Afternoon));
      portuguese.AddFacet("GoodNight",
                          Repository.GetRepositoryID(typeof (Greetings)),
                          new GreetingsImpl(conn2,
                                            GreetingsImpl.Languages.Portuguese,
                                            GreetingsImpl.Period.Night));

      // Define propriedade para as ofertas de serviço a serem registradas no barramento
      ServiceProperty[] properties = new[] {
                                             new ServiceProperty("offer.domain",
                                                                 "OpenBus Demos")
                                           };
      IComponent englishIC = english.GetIComponent();
      IComponent spanishIC = spanish.GetIComponent();
      IComponent portugueseIC = portuguese.GetIComponent();

      // adiciona as callbacks de recuperação de login
      conn.OnInvalidLogin = new MultiplexingInvalidLoginCallback(entity,
                                                                 privateKey,
                                                                 englishIC,
                                                                 properties);
      conn2.OnInvalidLogin = new MultiplexingInvalidLoginCallback(entity,
                                                                  privateKey,
                                                                  spanishIC,
                                                                  properties);
      conn3.OnInvalidLogin = new MultiplexingInvalidLoginCallback(entity,
                                                                  privateKey,
                                                                  portugueseIC,
                                                                  properties);

      // Registra as ofertas no barramento das três conexões
      if (!Register(conn, englishIC, properties)) {
        Console.ReadLine();
        Environment.Exit(1);
      }
      if (!Register(conn2, spanishIC, properties)) {
        Console.ReadLine();
        Environment.Exit(1);
      }
      if (!Register(conn3, portugueseIC, properties)) {
        Console.ReadLine();
        Environment.Exit(1);
      }

      // Mantém a thread ativa para aguardar requisições
      Console.WriteLine("Servidor no ar.");
      Thread.Sleep(Timeout.Infinite);
    }

    internal static bool Register(Connection conn, IComponent component,
                                  ServiceProperty[] properties) {
      try {
        // Seta a conexão a ser usada para essa chamada
        ConnectionManager manager = ORBInitializer.Manager;
        manager.Requester = conn;
        Offers.Add(conn, conn.Offers.registerService(component, properties));
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

    internal static bool Login(Connection conn, string entity, byte[] privateKey) {
      try {
        conn.LoginByCertificate(entity, privateKey);
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
      if (Offers.Count == 0) {
        return;
      }
      Console.WriteLine(
        "Removendo ofertas do barramento antes de terminar o processo...");
      ConnectionManager manager = ORBInitializer.Manager;
      foreach (KeyValuePair<Connection, ServiceOffer> kvp in Offers) {
        try {
          // ativa a conexão correta
          manager.Requester = kvp.Key;
          // remove a oferta
          kvp.Value.remove();
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
      }
    }
  }
}