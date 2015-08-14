using System;
using System.Threading;
using Ch.Elca.Iiop.Idl;
using Scs.Core;
using demo.Properties;
using Org.BouncyCastle.Crypto;
using scs.core;
using tecgraf.openbus.assistant;
using tecgraf.openbus.core.v2_0.services.offer_registry;
using tecgraf.openbus.security;

namespace demo {
  /// <summary>
  /// Servidor da demo CallChain.
  /// </summary>
  internal static class Server {
    private static Assistant _assistant;

    private static void Main(String[] args) {
      // Registra handler para o caso do processo ser finalizado
      AppDomain.CurrentDomain.ProcessExit += CurrentDomainProcessExit;

      // Obtém dados através dos argumentos
      string host = args[0];
      ushort port = Convert.ToUInt16(args[1]);
      string entity = args[2];
      AsymmetricCipherKeyPair privateKey = Crypto.ReadKeyFile(args[3]);

      // Cria o componente que conterá as facetas do servidor
      ComponentContext component =
        new DefaultComponentContext(new ComponentId("messenger", 1, 0, 0, ".net"));

      // Cria a faceta Messenger para o componente
      component.AddFacet("Messenger",
                         Repository.GetRepositoryID(typeof (Messenger)),
                         new MessengerImpl(entity));

      // Define propriedades para a oferta de serviço a ser registrada no barramento
      IComponent ic = component.GetIComponent();
      ServiceProperty[] properties = {
                                       new ServiceProperty("offer.domain",
                                                           "Demo CallChain"),
                                       new ServiceProperty("offer.role",
                                                           "mensageiro real")
                                      };

      // Usa o assistente do OpenBus para se conectar ao barramento, realizar a autenticação e se registrar.
      AssistantProperties props = new PrivateKeyProperties(entity, privateKey) {
        LoginFailureCallback = LoginFailureCallback,
        RegisterFailureCallback = RegisterFailureCallback
      };
      _assistant = new AssistantImpl(host, port, props);
      _assistant.RegisterService(ic, properties);

      // Mantém a thread ativa para aguardar requisições
      Console.WriteLine(Resources.ServerOK);
      Thread.Sleep(Timeout.Infinite);
    }

    private static void RegisterFailureCallback(Assistant assistant, IComponent component, ServiceProperty[] props, Exception e) {
      Console.WriteLine(Resources.RegisterFailureCallback + e);
    }

    private static void LoginFailureCallback(Assistant assistant, Exception e) {
      Console.WriteLine(Resources.LoginFailureCallback + e);
    }

    private static void CurrentDomainProcessExit(object sender, EventArgs e) {
      _assistant.Shutdown();
    }
  }
}