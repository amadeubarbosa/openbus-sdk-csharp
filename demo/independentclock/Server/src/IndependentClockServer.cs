using System;
using System.IO;
using System.Threading;
using Ch.Elca.Iiop.Idl;
using Scs.Core;
using omg.org.CORBA;
using scs.core;
using tecgraf.openbus;
using tecgraf.openbus.core.v2_00.services;
using tecgraf.openbus.core.v2_00.services.access_control;
using tecgraf.openbus.core.v2_00.services.offer_registry;
using tecgraf.openbus.exceptions;

namespace Server {
  /// <summary>
  /// Servidor da demo Independent Clock.
  /// </summary>
  internal static class IndependentClockServer {
    private static Connection _conn;
    private static ServiceOffer _offer;
    private static int _waitTime;
    private static string _host;
    private static short _port;
    private static string _entity;
    private static byte[] _key;

    private static void Main(String[] args) {
      // Obtém dados através dos argumentos
      _host = args[0];
      _port = Convert.ToInt16(args[1]);
      _entity = args[2];
      _key = File.ReadAllBytes(args[3]);
      _waitTime = Convert.ToInt32(args[4]);

      // Inicia thread que tenta conectar ao barramento
      ThreadStart ts = ConnectToOpenBus;
      Thread t = new Thread(ts) {IsBackground = true};
      t.Start();

      // Realiza trabalho independente do OpenBus
      while (true) {
        Console.WriteLine(String.Format("Hora atual: {0:HH:mm:ss}", DateTime.Now));
        Thread.Sleep(_waitTime);
      }
    }

    private static void ConnectToOpenBus() {
      // Registra handler para o caso do processo ser finalizado
      AppDomain.CurrentDomain.ProcessExit += CurrentDomainProcessExit;

      // Cria o componente que conterá as facetas do servidor
      ComponentContext component =
        new DefaultComponentContext(new ComponentId("independentclock", 1, 0, 0,
                                                    ".net"));

      // Cria a faceta Clock para o componente
      component.AddFacet("Clock", Repository.GetRepositoryID(typeof (Clock)),
                         new ClockImpl());

      // Define propriedades para a oferta de serviço a ser registrada no barramento
      IComponent ic = component.GetIComponent();
      ServiceProperty[] properties = new[]
                                     {
                                       new ServiceProperty("offer.domain",
                                                           "OpenBus Demos")
                                     };

      // Tenta eternamente fazer o login e registrar as ofertas
      TryLoginAndRegisterForever(ic, properties);

      // Mantém a thread ativa para aguardar requisições
      Console.WriteLine("Servidor no ar.");
      Thread.Sleep(Timeout.Infinite);
    }

    internal static bool TryLoginAndRegisterForever(IComponent ic,
                                                    ServiceProperty[] properties) {
      // tenta conectar, fazer login E registrar ofertas. Se o registro falhar devido à conexão ou login ter sido perdido, o procedimento deve ser reiniciado.
      bool firstTime = true;
      while (true) {
        // Se não for a primeira vez, espera
        if (firstTime) {
          firstTime = false;
        }
        else {
          Console.WriteLine(String.Format("Aguardando {0} milisegundos.",
                                          _waitTime));
          Thread.Sleep(_waitTime);
        }

        // Tenta conectar
        if (_conn == null) {
          if (!Connect(ic, properties)) {
            continue;
          }
        }

        // tenta fazer o login
        if (!Login()) {
          continue;
        }

        // tenta registrar as ofertas
        if (Register(ic, properties)) {
          return true;
        }
        // se chegou aqui não conseguiu porque o login ou a conexão foi perdida, então volta pro começo
      }
    }

    private static bool Register(IComponent ic, ServiceProperty[] properties) {
      try {
        _offer = _conn.Offers.registerService(ic, properties);
        return true;
      }
      catch (InvalidService) {
        Console.WriteLine(
          "Erro ao tentar registrar a oferta no barramento: o IComponent fornecido não é válido, por não apresentar facetas padrão definidas pelo modelo de componentes SCS.");
      }
      catch (InvalidProperties) {
        Console.WriteLine(
          "Erro ao tentar registrar a oferta no barramento: A lista de propriedades fornecida inclui propriedades inválidas, tais como propriedades com nomes reservados (cujos nomes começam com 'openbus.').");
      }
      catch (UnauthorizedFacets) {
        Console.WriteLine(
          "Erro ao tentar registrar a oferta no barramento: O componente que implementa o serviço apresenta facetas com interfaces que não estão autorizadas para a entidade realizando o registro da oferta de serviço.");
      }
      catch (NO_PERMISSION e) {
        if (e.Minor.Equals(NoLoginCode.ConstVal)) {
          // o login foi perdido, precisa tentar fazer o login novamente
          return false;
        }
      }
      catch (Exception e) {
        Console.WriteLine(
          "Erro inesperado ao tentar registrar a oferta no barramento:");
        Console.WriteLine(e);
        _conn = null;
      }
      return false;
    }

    private static bool Login() {
      if (_conn.Login != null) {
        return true;
      }
      try {
        _conn.LoginByCertificate(_entity, _key);
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
          _entity);
      }
      catch (ServiceFailure e) {
        Console.WriteLine(
          "Erro ao tentar realizar o login por certificado no barramento: Falha no serviço remoto. Causa:");
        Console.WriteLine(e);
      }
      catch (TRANSIENT) {
        Console.WriteLine(
          "Erro: O barramento não está acessível para realizar o login.");
        _conn = null;
      }
      catch (Exception e) {
        Console.WriteLine(
          "Erro inesperado ao tentar realizar o login por certificado no barramento:");
        Console.WriteLine(e);
        _conn = null;
      }
      return false;
    }

    private static bool Connect(IComponent ic, ServiceProperty[] properties) {
      // Cria conexão e a define como conexão padrão tanto para entrada como saída.
      // O uso exclusivo da conexão padrão (sem uso de requester e dispatcher) só é recomendado para aplicações que criem apenas uma conexão e desejem utilizá-la em todos os casos. Para situações diferentes, consulte o manual do SDK OpenBus e/ou outras demos.
      ConnectionManager manager = ORBInitializer.Manager;
      try {
        _conn = manager.CreateConnection(_host, _port);
        manager.DefaultConnection = _conn;
        // Registra uma callback para o caso do login ser perdido
        _conn.OnInvalidLogin = new IndependentClockInvalidLoginCallback(ic,
                                                                        properties);
        return true;
      }
      catch (TRANSIENT) {
        Console.WriteLine("O barramento não está acessível.");
      }
      catch (Exception e) {
        Console.WriteLine("Erro inesperado ao tentar conectar ao barramento:");
        Console.WriteLine(e);
      }
      _conn = null;
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
    }
  }
}