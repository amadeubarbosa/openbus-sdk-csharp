using System;
using System.Collections.Generic;
using System.Threading;
using Ch.Elca.Iiop.Idl;
using Scs.Core;
using demo.Properties;
using omg.org.CORBA;
using scs.core;
using tecgraf.openbus;
using tecgraf.openbus.core.v2_1.services;
using tecgraf.openbus.core.v2_1.services.access_control;
using tecgraf.openbus.core.v2_1.services.offer_registry;
using tecgraf.openbus.security;

namespace demo {
  /// <summary>
  /// Servidor da demo greetings.
  /// </summary>
  internal static class GreetingsServer {
    private static Connection _conn;

    private static readonly IList<ServiceOffer> Offers =
      new List<ServiceOffer>();

    private static void Main(String[] args) {
      // Registra handler para o caso do processo ser finalizado
      AppDomain.CurrentDomain.ProcessExit += CurrentDomainProcessExit;

      // Obtém dados através dos argumentos
      string host = args[0];
      ushort port = Convert.ToUInt16(args[1]);
      string entity = args[2];
      PrivateKey privateKey = Crypto.ReadKeyFile(args[3]);

      // Cria o componente que responde em inglês
      ComponentContext english =
        new DefaultComponentContext(new ComponentId("english", 1, 0, 0, ".net"));
      english.AddFacet("GoodMorning",
                       Repository.GetRepositoryID(typeof (Greetings)),
                       new GreetingsImpl(GreetingsImpl.Language.English,
                                         GreetingsImpl.Period.Morning));
      english.AddFacet("GoodAfternoon",
                       Repository.GetRepositoryID(typeof (Greetings)),
                       new GreetingsImpl(GreetingsImpl.Language.English,
                                         GreetingsImpl.Period.Afternoon));
      english.AddFacet("GoodNight",
                       Repository.GetRepositoryID(typeof (Greetings)),
                       new GreetingsImpl(GreetingsImpl.Language.English,
                                         GreetingsImpl.Period.Night));

      // Cria o componente que responde em espanhol
      ComponentContext spanish =
        new DefaultComponentContext(new ComponentId("spanish", 1, 0, 0, ".net"));
      spanish.AddFacet("GoodMorning",
                       Repository.GetRepositoryID(typeof (Greetings)),
                       new GreetingsImpl(GreetingsImpl.Language.Spanish,
                                         GreetingsImpl.Period.Morning));
      spanish.AddFacet("GoodAfternoon",
                       Repository.GetRepositoryID(typeof (Greetings)),
                       new GreetingsImpl(GreetingsImpl.Language.Spanish,
                                         GreetingsImpl.Period.Afternoon));
      spanish.AddFacet("GoodNight",
                       Repository.GetRepositoryID(typeof (Greetings)),
                       new GreetingsImpl(GreetingsImpl.Language.Spanish,
                                         GreetingsImpl.Period.Night));

      // Cria o componente que responde em português
      ComponentContext portuguese =
        new DefaultComponentContext(new ComponentId("portuguese", 1, 0, 0,
                                                    ".net"));
      portuguese.AddFacet("GoodMorning",
                          Repository.GetRepositoryID(typeof (Greetings)),
                          new GreetingsImpl(GreetingsImpl.Language.Portuguese,
                                            GreetingsImpl.Period.Morning));
      portuguese.AddFacet("GoodAfternoon",
                          Repository.GetRepositoryID(typeof (Greetings)),
                          new GreetingsImpl(GreetingsImpl.Language.Portuguese,
                                            GreetingsImpl.Period.Afternoon));
      portuguese.AddFacet("GoodNight",
                          Repository.GetRepositoryID(typeof (Greetings)),
                          new GreetingsImpl(GreetingsImpl.Language.Portuguese,
                                            GreetingsImpl.Period.Night));

      // Define propriedade para as ofertas de serviço a serem registradas no barramento
      ServiceProperty[] properties = new[] {
                                             new ServiceProperty("offer.domain",
                                                                 "Demo Greetings")
                                           };

      // Cria conexão e a define como conexão padrão tanto para entrada como saída.
      // O uso exclusivo da conexão padrão (sem uso de current e callback de despacho) só é recomendado para aplicações que criem apenas uma conexão e desejem utilizá-la em todos os casos. Para situações diferentes, consulte o manual do SDK OpenBus e/ou outras demos.
      OpenBusContext context = ORBInitializer.Context;
      _conn = context.CreateConnection(host, port, null);
      context.SetDefaultConnection(_conn);

      bool failed = true;
      try {
        // Faz o login
        _conn.LoginByCertificate(entity, privateKey);
        // Registra as ofertas no barramento
        OfferRegistry registry = context.OfferRegistry;
        Offers.Add(registry.registerService(english.GetIComponent(), properties));
        Offers.Add(registry.registerService(spanish.GetIComponent(), properties));
        Offers.Add(registry.registerService(portuguese.GetIComponent(),
                                             properties));
        failed = false;
      }
        // Login
      catch (AccessDenied) {
        Console.WriteLine(Resources.ServerAccessDenied);
      }
      catch (MissingCertificate) {
        Console.WriteLine(Resources.MissingCertificateForEntity + entity);
      }
        // Registro
      catch (UnauthorizedFacets) {
        Console.WriteLine(Resources.UnauthorizedFacets);
      }
        // Barramento
      catch (ServiceFailure e) {
        Console.WriteLine(Resources.BusServiceFailureErrorMsg);
        Console.WriteLine(e);
      }
      catch (TRANSIENT) {
        Console.WriteLine(Resources.BusTransientErrorMsg);
      }
      catch (COMM_FAILURE) {
        Console.WriteLine(Resources.BusCommFailureErrorMsg);
      }
      catch (NO_PERMISSION e) {
        if (e.Minor == NoLoginCode.ConstVal) {
          Console.WriteLine(Resources.NoLoginCodeErrorMsg);
        }
        else {
          throw;
        }
      }
      finally {
        if (failed) {
          Exit(1);
        }
      }

      // Mantém a thread ativa para aguardar requisições
      Console.WriteLine(Resources.ServerOK);
      Thread.Sleep(Timeout.Infinite);
    }

    private static void RemoveOfferAndLogout() {
      if (Offers.Count > 0) {
        foreach (ServiceOffer offer in Offers) {
          try {
            Console.WriteLine(Resources.RemovingOffer);
            offer.remove();
            Console.WriteLine(Resources.RemovedOffer);
          }
          catch (UnauthorizedOperation) {
            Console.WriteLine(Resources.UnauthorizedRemoveOffer);
          }
          catch (ServiceFailure e) {
            Console.WriteLine(Resources.BusServiceFailureErrorMsg);
            Console.WriteLine(e);
          }
          catch (TRANSIENT) {
            Console.WriteLine(Resources.BusTransientErrorMsg);
          }
          catch (COMM_FAILURE) {
            Console.WriteLine(Resources.BusCommFailureErrorMsg);
          }
          catch (NO_PERMISSION e) {
            if (e.Minor == NoLoginCode.ConstVal) {
              Console.WriteLine(Resources.NoLoginCodeErrorMsg);
            }
            else {
              throw;
            }
          }
        }
      }
      if (_conn.Login.HasValue) {
        try {
          _conn.Logout();
        }
        catch (ServiceFailure e) {
          Console.WriteLine(Resources.BusServiceFailureErrorMsg);
          Console.WriteLine(e);
        }
        catch (TRANSIENT) {
          Console.WriteLine(Resources.BusTransientErrorMsg);
        }
        catch (COMM_FAILURE) {
          Console.WriteLine(Resources.BusCommFailureErrorMsg);
        }
        catch (NO_PERMISSION e) {
          if (e.Minor == NoLoginCode.ConstVal) {
            Console.WriteLine(Resources.NoLoginCodeErrorMsg);
          }
          else {
            throw;
          }
        }
      }
    }

    private static void CurrentDomainProcessExit(object sender, EventArgs e) {
      RemoveOfferAndLogout();
    }

    private static void Exit(int code) {
      RemoveOfferAndLogout();
      Console.WriteLine(Resources.PressAnyKeyToExit);
      Console.ReadKey();
      Environment.Exit(code);
    }
  }
}