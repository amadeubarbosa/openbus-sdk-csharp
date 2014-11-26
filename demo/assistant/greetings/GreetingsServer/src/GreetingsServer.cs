using System;
using System.Threading;
using Ch.Elca.Iiop.Idl;
using Scs.Core;
using demo.Properties;
using scs.core;
using tecgraf.openbus;
using tecgraf.openbus.assistant;
using tecgraf.openbus.core.v2_0.services.offer_registry;
using tecgraf.openbus.security;

namespace demo {
  /// <summary>
  /// Servidor da demo greetings.
  /// </summary>
  internal static class GreetingsServer {
    private static Assistant _assistant;

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
      ServiceProperty[] properties = {
                                       new ServiceProperty("offer.domain",
                                                           "Demo Greetings")
                                     };

      // Usa o assistente do OpenBus para se conectar ao barramento, realizar a autenticação e registrar todos os componentes.
      AssistantProperties props = new PrivateKeyProperties(entity, privateKey) {
        LoginFailureCallback = LoginFailureCallback,
        RegisterFailureCallback = RegisterFailureCallback
      };
      _assistant = new AssistantImpl(host, port, props);
      _assistant.RegisterService(english.GetIComponent(), properties);
      _assistant.RegisterService(spanish.GetIComponent(), properties);
      _assistant.RegisterService(portuguese.GetIComponent(), properties);

      // Mantém a thread ativa para aguardar requisições
      Console.WriteLine(Resources.ServerOK);
      Thread.Sleep(Timeout.Infinite);
    }

    private static void RegisterFailureCallback(Assistant assistant, IComponent component, ServiceProperty[] props, Exception e) {
      Console.WriteLine(Resources.RegisterFailureCallback + Utils.GetProperty(props, "openbus.component.name") + ": " + e);
    }

    private static void LoginFailureCallback(Assistant assistant, Exception e) {
      Console.WriteLine(Resources.LoginFailureCallback + e);
    }

    private static void CurrentDomainProcessExit(object sender, EventArgs e) {
      _assistant.Shutdown();
    }
  }
}