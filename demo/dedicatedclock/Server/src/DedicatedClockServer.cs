using System;
using System.IO;
using System.Threading;
using Ch.Elca.Iiop.Idl;
using Scs.Core;
using omg.org.CORBA;
using scs.core;
using tecgraf.openbus;
using tecgraf.openbus.core.v2_0.services;
using tecgraf.openbus.core.v2_0.services.access_control;
using tecgraf.openbus.core.v2_0.services.offer_registry;
using tecgraf.openbus.exceptions;

namespace Server {
  /// <summary>
  /// Servidor da demo Dedicated Clock.
  /// </summary>
  internal static class DedicatedClockServer {
    private static Connection _conn;
    private static ServiceOffer _offer;

    private static void Main(String[] args) {
      // Registra handler para o caso do processo ser finalizado
      AppDomain.CurrentDomain.ProcessExit += CurrentDomainProcessExit;

      // Obt�m dados atrav�s dos argumentos
      string host = args[0];
      short port = Convert.ToInt16(args[1]);
      string entity = args[2];
      string key = args[3];
      int waitTime = Convert.ToInt32(args[4]);

      // L� a chave privada de um arquivo
      byte[] privateKey = File.ReadAllBytes(key);

      // Cria o componente que conter� as facetas do servidor
      ComponentContext component =
        new DefaultComponentContext(new ComponentId("dedicatedclock", 1, 0, 0,
                                                    ".net"));

      // Cria a faceta Clock para o componente
      component.AddFacet("Clock", Repository.GetRepositoryID(typeof (Clock)),
                         new ClockImpl());

      // Define propriedades para a oferta de servi�o a ser registrada no barramento
      IComponent ic = component.GetIComponent();
      ServiceProperty[] properties = new[]
                                     {
                                       new ServiceProperty("offer.domain",
                                                           "OpenBus Demos")
                                     };

      // Tenta eternamente fazer o login e registrar as ofertas
      TryLoginAndRegisterForever(host, port, entity, privateKey, ic, properties,
                                 waitTime);

      // Mant�m a thread ativa para aguardar requisi��es
      Console.WriteLine("Servidor no ar.");
      Thread.Sleep(Timeout.Infinite);
    }

    internal static void TryLoginAndRegisterForever(string host,
                                                    short port,
                                                    string entity,
                                                    byte[] privateKey,
                                                    IComponent ic,
                                                    ServiceProperty[] properties,
                                                    int waitTime) {
      // tenta conectar, fazer login E registrar ofertas. Se o registro falhar devido � conex�o ou login ter sido perdido, o procedimento deve ser reiniciado.
      bool firstTime = true;
      while (true) {
        // Se n�o for a primeira vez, espera
        if (firstTime) {
          firstTime = false;
        }
        else {
          Console.WriteLine(String.Format("Aguardando {0} milisegundos.", waitTime));
          Thread.Sleep(waitTime);
        }

        // Tenta conectar
        if (_conn == null) {
          if (!Connect(host, port, entity, privateKey, ic, properties, waitTime)) {
            continue;
          }
        }

        // tenta fazer o login
        if (!Login(entity, privateKey)) {
          continue;
        }

        // tenta registrar as ofertas
        if (Register(ic, properties)) {
          return;
        }
        // se chegou aqui n�o conseguiu porque o login ou a conex�o foi perdida, ent�o volta pro come�o
      }
    }

    private static bool Register(IComponent ic, ServiceProperty[] properties) {
      try {
        _offer = _conn.Offers.registerService(ic, properties);
        return true;
      }
      catch (NO_PERMISSION e) {
        if (e.Minor.Equals(NoLoginCode.ConstVal)) {
          // o login foi perdido, precisa tentar fazer o login novamente
          return false;
        }
      }
      catch (InvalidService) {
        Console.WriteLine(
          "Erro ao tentar registrar a oferta no barramento: o IComponent fornecido n�o � v�lido, por n�o apresentar facetas padr�o definidas pelo modelo de componentes SCS.");
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
        Console.WriteLine(e);
        _conn = null;
      }
      return false;
    }

    private static bool Login(string entity, byte[] privateKey) {
      if (_conn.Login != null) {
        return true;
      }
      try {
        _conn.LoginByCertificate(entity, privateKey);
        return true;
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
        Console.WriteLine(e);
      }
      catch (TRANSIENT) {
        Console.WriteLine(
          "Erro: O barramento n�o est� acess�vel para realizar o login.");
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

    private static bool Connect(string host, short port, string entity,
                                byte[] privateKey, IComponent ic,
                                ServiceProperty[] properties, int waitTime) {
      // Cria conex�o e a define como conex�o padr�o tanto para entrada como sa�da.
      // O uso exclusivo da conex�o padr�o (sem uso de requester e dispatcher) s� � recomendado para aplica��es que criem apenas uma conex�o e desejem utiliz�-la em todos os casos. Para situa��es diferentes, consulte o manual do SDK OpenBus e/ou outras demos.
      ConnectionManager manager = ORBInitializer.Manager;
      try {
        _conn = manager.CreateConnection(host, port);
        manager.DefaultConnection = _conn;
        // Registra uma callback para o caso do login ser perdido
        _conn.OnInvalidLogin = new DedicatedClockInvalidLoginCallback(host,
                                                                      port,
                                                                      entity,
                                                                      privateKey,
                                                                      ic,
                                                                      properties,
                                                                      waitTime);
        return true;
      }
      catch (TRANSIENT) {
        Console.WriteLine("O barramento n�o est� acess�vel.");
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
          "Erro ao tentar remover a oferta do barramento: opera��o n�o autorizada. O login utilizado para remover a oferta n�o � o mesmo que a registrou e n�o � um administrador do barramento.");
      }
      catch (ServiceFailure exc) {
        Console.WriteLine(
          "Erro ao tentar remover a oferta do barramento: erro no servi�o remoto. Causa:");
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