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

      // Cria conex�o e a define como conex�o padr�o tanto para entrada como sa�da.
      // O uso exclusivo da conex�o padr�o (sem uso de requester e dispatcher) s� � recomendado para aplica��es que criem apenas uma conex�o e desejem utiliz�-la em todos os casos. Para situa��es diferentes, consulte o manual do SDK OpenBus e/ou outras demos.
      ConnectionManager manager = ORBInitializer.Manager;
      _conn = manager.CreateConnection("localhost", 2089);
      manager.DefaultConnection = _conn;

      // L� a chave privada de um arquivo
      byte[] privateKey = File.ReadAllBytes("DemoGreetings.key");

      // Cria o componente que responde em ingl�s
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

      // Cria o componente que responde em portugu�s
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

      // Define propriedade para as ofertas de servi�o a serem registradas no barramento
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

      // Mant�m a thread ativa para aguardar requisi��es
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
          "Falha ao tentar realizar o login por certificado no barramento: a entidade j� est� com o login realizado. Esta falha ser� ignorada.");
        return true;
      }
      catch (CorruptedPrivateKeyException) {
        Console.WriteLine(
          "Erro ao tentar realizar o login por certificado no barramento: a chave privada est� corrompida ou em um formato errado.");
      }
      catch (WrongPrivateKeyException) {
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
            "Erro ao tentar remover a oferta do barramento: opera��o n�o autorizada. O login utilizado para remover a oferta n�o � o mesmo que a registrou e n�o � um administrador do barramento.");
        }
        catch (ServiceFailure exc) {
          Console.WriteLine(
            "Erro ao tentar remover a oferta do barramento: erro no servi�o remoto. Causa:");
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