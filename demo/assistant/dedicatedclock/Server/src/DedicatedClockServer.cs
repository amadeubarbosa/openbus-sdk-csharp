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
  /// Servidor da demo Dedicated Clock.
  /// </summary>
  internal static class DedicatedClockServer {
    private static Assistant _assistant;

    private static void Main(String[] args) {
      // Registra handler para o caso do processo ser finalizado
      AppDomain.CurrentDomain.ProcessExit += CurrentDomainProcessExit;

      // Obtém dados através dos argumentos
      string host = args[0];
      ushort port = Convert.ToUInt16(args[1]);
      string entity = args[2];
      PrivateKey privateKey = Crypto.ReadKeyFile(args[3]);
      int interval = Convert.ToInt32(args.Length > 4 ? args[4] : "1");

      // Cria o componente que conterá as facetas do servidor
      ComponentContext component =
        new DefaultComponentContext(new ComponentId("dedicatedclock", 1, 0, 0,
                                                    ".net"));

      // Cria a faceta Clock para o componente
      component.AddFacet("Clock", Repository.GetRepositoryID(typeof (Clock)),
                         new ClockImpl());

      // Define propriedades para a oferta de serviço a ser registrada no barramento
      IComponent ic = component.GetIComponent();
      ServiceProperty[] properties = new[] {
                                             new ServiceProperty("offer.domain",
                                                                 "Demo Dedicated Clock")
                                           };

      // Usa o assistente do OpenBus para se conectar ao barramento, realizar a autenticação e se registrar.
      AssistantProperties assistantProps = new PrivateKeyProperties(entity,
                                                                    privateKey)
                                           {Interval = interval};
      _assistant = new AssistantImpl(host, port, assistantProps);
      _assistant.RegisterService(ic, properties);

      // Mantém a thread ativa para aguardar requisições
      Console.WriteLine(Resources.ServerOK);
      Thread.Sleep(Timeout.Infinite);
    }

    private static void CurrentDomainProcessExit(object sender, EventArgs e) {
      _assistant.Shutdown();
    }
  }
}