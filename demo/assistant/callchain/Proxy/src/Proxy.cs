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
  /// Proxy da demo CallChain.
  /// </summary>
  internal static class Proxy {
    private static Assistant _assistant;
    internal static ServiceOfferDesc[] Offers;

    private static void Main(String[] args) {
      // Registra handler para o caso do processo ser finalizado
      AppDomain.CurrentDomain.ProcessExit += CurrentDomainProcessExit;

      // Obtém dados através dos argumentos
      string host = args[0];
      ushort port = Convert.ToUInt16(args[1]);
      string entity = args[2];
      PrivateKey privateKey = Crypto.ReadKeyFile(args[3]);

      // Cria o componente que conterá as facetas do servidor
      ComponentContext component =
        new DefaultComponentContext(new ComponentId("messengerproxy", 1, 0, 0,
                                                    ".net"));

      // Cria a faceta Messenger para o componente
      string messengerIDLType = Repository.GetRepositoryID(typeof (Messenger));
      component.AddFacet("Messenger", messengerIDLType, new ProxyMessengerImpl());

      // Define propriedades para a oferta de serviço a ser registrada no barramento
      IComponent ic = component.GetIComponent();
      ServiceProperty prop1 = new ServiceProperty("offer.domain",
                                                  "Demo CallChain");
      ServiceProperty prop2 = new ServiceProperty("offer.role",
                                                  "mensageiro proxy");
      ServiceProperty[] properties = new[] {prop1, prop2};

      // Usa o assistente do OpenBus para se conectar ao barramento e realizar a autenticação.
      _assistant = new AssistantImpl(host, port,
                                     new PrivateKeyProperties(entity, privateKey));

      // Busca o Messenger real
      ServiceProperty autoProp =
        new ServiceProperty("openbus.component.interface", messengerIDLType);
      ServiceProperty findProp = new ServiceProperty("offer.role",
                                                     "mensageiro real");
      Offers =
        Utils.FilterWorkingOffers(
          _assistant.FindServices(new[] {autoProp, findProp, prop1}, 10));

      // Registra a própria oferta no barramento
      _assistant.RegisterService(ic, properties);

      // Mantém a thread ativa para aguardar requisições
      Console.WriteLine(Resources.CallChainProxyOK);
      Thread.Sleep(Timeout.Infinite);
    }

    private static void CurrentDomainProcessExit(object sender, EventArgs e) {
      _assistant.Shutdown();
    }
  }
}