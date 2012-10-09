using System;
using System.Threading;
using Ch.Elca.Iiop.Idl;
using Scs.Core;
using scs.core;
using tecgraf.openbus;
using tecgraf.openbus.assistant;
using tecgraf.openbus.core.v2_0.services.offer_registry;
using tecgraf.openbus.security;

namespace demo {
  /// <summary>
  /// Servidor da demo Independent Clock.
  /// </summary>
  internal static class IndependentClockServer {
    private static Assistant _assistant;

    private static void Main(String[] args) {
      // Obtém dados através dos argumentos
      string host = args[0];
      ushort port = Convert.ToUInt16(args[1]);
      string entity = args[2];
      PrivateKey privateKey = Crypto.ReadKeyFile(args[3]);
      int interval = Convert.ToInt32(args.Length > 4 ? args[4] : "1");

      // Registra handler para o caso do processo ser finalizado
      AppDomain.CurrentDomain.ProcessExit += CurrentDomainProcessExit;

      // Cria o componente que conterá as facetas do servidor
      ComponentContext component =
        new DefaultComponentContext(new ComponentId("independentclock", 1, 0, 0,
                                                    ".net"));

      // Cria a faceta Clock para o componente
      ClockImpl clock = new ClockImpl();
      component.AddFacet("Clock", Repository.GetRepositoryID(typeof (Clock)),
                         clock);

      // Define propriedades para a oferta de serviço a ser registrada no barramento
      ServiceProperty[] properties = new[] {
                                             new ServiceProperty("offer.domain",
                                                                 "Demo Independent Clock")
                                           };

      // Usa o assistente do OpenBus para se conectar ao barramento, realizar a autenticação e se registrar.
      _assistant = new AssistantImpl(host, port,
                                     new PrivateKeyProperties(entity, privateKey));
      _assistant.RegisterService(component.GetIComponent(), properties);

      // Realiza trabalho independente do OpenBus
      while (true) {
        clock.getTimeInTicks();
        //        Console.WriteLine(String.Format("Hora atual: {0:HH:mm:ss}", DateTime.Now));
        Thread.Sleep(interval);
      }
    }

    private static void CurrentDomainProcessExit(object sender, EventArgs e) {
      _assistant.Shutdown();
    }
  }
}