using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Ch.Elca.Iiop.Idl;
using Scs.Core;
using scs.core;
using tecgraf.openbus.core.v2_00.services;
using tecgraf.openbus.core.v2_00.services.access_control;
using tecgraf.openbus.core.v2_00.services.offer_registry;
using tecgraf.openbus.exceptions;

namespace tecgraf.openbus.demo.greetings {
  /// <summary>
  /// Servidor da demo greetings.
  /// </summary>
  internal static class GreetingsServer {
    private static Connection _conn;
    private static readonly IList<ServiceOffer> Offers = new List<ServiceOffer>();

    private static void Main() {
      // Registra handler para o caso do processo ser finalizado
      AppDomain.CurrentDomain.ProcessExit += CurrentDomainProcessExit;

      // Cria conexão e a define como conexão padrão tanto para entrada como saída.
      // O uso exclusivo da conexão padrão (sem uso de requester e dispatcher) só é recomendado para aplicações que criem apenas uma conexão e desejem utilizá-la em todos os casos. Para situações diferentes, consulte o manual do SDK OpenBus e/ou outras demos.
      ConnectionManager manager = ORBInitializer.Manager;
      _conn = manager.CreateConnection("localhost", 2089);
      manager.DefaultConnection = _conn;

      // Lê a chave privada de um arquivo
      byte[] privateKey = File.ReadAllBytes("DemoGreetings.key");

      // Cria o componente que responde em inglês
      ComponentContext english =
        new DefaultComponentContext(new ComponentId("english", 1, 0, 0, ".net"));
      english.AddFacet("GoodMorning",
                       Repository.GetRepositoryID(typeof (Greetings)),
                       new GreetingsImpl(_conn, GreetingsImpl.Languages.English,
                                         GreetingsImpl.Period.Morning));
      english.AddFacet("GoodAfternoon",
                       Repository.GetRepositoryID(typeof (Greetings)),
                       new GreetingsImpl(_conn, GreetingsImpl.Languages.English,
                                         GreetingsImpl.Period.Afternoon));
      english.AddFacet("GoodNight",
                       Repository.GetRepositoryID(typeof (Greetings)),
                       new GreetingsImpl(_conn, GreetingsImpl.Languages.English,
                                         GreetingsImpl.Period.Night));

      // Cria o componente que responde em espanhol
      ComponentContext spanish =
        new DefaultComponentContext(new ComponentId("spanish", 1, 0, 0, ".net"));
      spanish.AddFacet("GoodMorning",
                       Repository.GetRepositoryID(typeof (Greetings)),
                       new GreetingsImpl(_conn, GreetingsImpl.Languages.Spanish,
                                         GreetingsImpl.Period.Morning));
      spanish.AddFacet("GoodAfternoon",
                       Repository.GetRepositoryID(typeof (Greetings)),
                       new GreetingsImpl(_conn, GreetingsImpl.Languages.Spanish,
                                         GreetingsImpl.Period.Afternoon));
      spanish.AddFacet("GoodNight",
                       Repository.GetRepositoryID(typeof (Greetings)),
                       new GreetingsImpl(_conn, GreetingsImpl.Languages.Spanish,
                                         GreetingsImpl.Period.Night));

      // Cria o componente que responde em português
      ComponentContext portuguese =
        new DefaultComponentContext(new ComponentId("portuguese", 1, 0, 0,
                                                    ".net"));
      portuguese.AddFacet("GoodMorning",
                          Repository.GetRepositoryID(typeof (Greetings)),
                          new GreetingsImpl(_conn,
                                            GreetingsImpl.Languages.Portuguese,
                                            GreetingsImpl.Period.Morning));
      portuguese.AddFacet("GoodAfternoon",
                          Repository.GetRepositoryID(typeof (Greetings)),
                          new GreetingsImpl(_conn,
                                            GreetingsImpl.Languages.Portuguese,
                                            GreetingsImpl.Period.Afternoon));
      portuguese.AddFacet("GoodNight",
                          Repository.GetRepositoryID(typeof (Greetings)),
                          new GreetingsImpl(_conn,
                                            GreetingsImpl.Languages.Portuguese,
                                            GreetingsImpl.Period.Night));

      // Define propriedade para as ofertas de serviço a serem registradas no barramento
      ServiceProperty[] properties = new[] {
                                             new ServiceProperty("offer.domain",
                                                                 "OpenBus Demos")
                                           };

      // Faz o login
      const string login = "GreetingsServer";
      if (!Login(login, privateKey)) {
        Console.ReadLine();
        Environment.Exit(1);
      }

      // Registra as ofertas no barramento
      IComponent[] components = new[] {
                                        english.GetIComponent(),
                                        spanish.GetIComponent(),
                                        portuguese.GetIComponent()
                                      };
      if (!Register(components, properties)) {
        Console.ReadLine();
        Environment.Exit(1);
      }

      // Registra uma callback para o caso do login ser perdido
      _conn.OnInvalidLogin = new GreetingsInvalidLoginCallback(login, privateKey,
                                                               components,
                                                               properties);

      // Mantém a thread ativa para aguardar requisições
      Console.WriteLine("Servidor no ar.");
      Thread.Sleep(Timeout.Infinite);
    }

    internal static bool Register(IEnumerable<IComponent> components,
                                  ServiceProperty[] properties) {
      foreach (IComponent component in components) {
        try {
          Offers.Add(_conn.Offers.registerService(component, properties));
          continue;
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
          Console.WriteLine(e.StackTrace);
        }
        return false;
      }
      return true;
    }

    internal static bool Login(string entity, byte[] privateKey) {
      try {
        _conn.LoginByCertificate(entity, privateKey);
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
        Console.WriteLine(e.StackTrace);
      }
      catch (Exception e) {
        Console.WriteLine(
          "Erro inesperado ao tentar realizar o login por certificado no barramento:");
        Console.WriteLine(e.StackTrace);
      }
      return false;
    }

    private static void CurrentDomainProcessExit(object sender, EventArgs e) {
      if (Offers.Count == 0) {
        return;
      }
      Console.WriteLine("Removendo ofertas do barramento antes de terminar o processo...");
      foreach (ServiceOffer serviceOffer in Offers) {
        try {
          serviceOffer.remove();
          Console.WriteLine("Oferta removida do barramento.");
        }
        catch (UnauthorizedOperation) {
          Console.WriteLine(
            "Erro ao tentar remover a oferta do barramento: operação não autorizada. O login utilizado para remover a oferta não é o mesmo que a registrou e não é um administrador do barramento.");
        }
        catch (ServiceFailure exc) {
          Console.WriteLine(
            "Erro ao tentar remover a oferta do barramento: erro no serviço remoto. Causa:");
          Console.WriteLine(exc.StackTrace);
        }
        catch (Exception exc) {
          Console.WriteLine(
            "Erro inesperado ao tentar remover a oferta do barramento:");
          Console.WriteLine(exc.StackTrace);
        }
      }
    }
  }
}