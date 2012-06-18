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
  /// Servidor da demo clock.
  /// </summary>
  internal static class DedicatedClockServer {
    private static Connection _conn;
    private static ServiceOffer _offer;

    private static void Main(String[] args) {
      // Registra handler para o caso do processo ser finalizado
      AppDomain.CurrentDomain.ProcessExit += CurrentDomainProcessExit;

      // Obtém dados através dos argumentos
      string host = args[0];
      short port = Convert.ToInt16(args[1]);
      string entity = args[2];
      string key = args[3];
      int waitTime = Convert.ToInt32(args[4]);

      // Lê a chave privada de um arquivo
      byte[] privateKey = File.ReadAllBytes(key);

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
                                                                 "OpenBus Demos")
                                           };

      // Cria conexão e a define como conexão padrão tanto para entrada como saída.
      // O uso exclusivo da conexão padrão (sem uso de requester e dispatcher) só é recomendado para aplicações que criem apenas uma conexão e desejem utilizá-la em todos os casos. Para situações diferentes, consulte o manual do SDK OpenBus e/ou outras demos.
      ConnectionManager manager = ORBInitializer.Manager;
      _conn = manager.CreateConnection(host, port);
      manager.DefaultConnection = _conn;

      // Tenta eternamente fazer o login e registrar as ofertas
      TryLoginAndRegisterForever(entity, privateKey, ic, properties, waitTime);

      // Registra uma callback para o caso do login ser perdido
      _conn.OnInvalidLogin = new DedicatedClockInvalidLoginCallback(
        entity, privateKey, ic, properties, waitTime);

      // Mantém a thread ativa para aguardar requisições
      Console.WriteLine("Servidor no ar.");
      Thread.Sleep(Timeout.Infinite);
    }

    internal static bool TryLoginAndRegisterForever(string entity,
                                                    byte[] privateKey,
                                                    IComponent ic,
                                                    ServiceProperty[] properties,
                                                    int waitTime) {
      // tenta fazer login E registrar ofertas. Se o registro falhar devido ao login ter sido perdido, o procedimento deve ser reiniciado.
      while (true) {
        // tenta fazer o login
        while (true) {
          if (Login(entity, privateKey)) {
            break;
          }
          Thread.Sleep(waitTime);
        }
        // tenta registrar as ofertas
        if (Register(ic, properties, waitTime)) {
          return true;
        }
        // se chegou aqui não conseguiu porque o login foi perdido, então volta pro começo
        Thread.Sleep(waitTime);
      }
    }

    private static bool Register(IComponent ic, ServiceProperty[] properties,
                                 int waitTime) {
      while (true) {
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
        catch (Exception e) {
          Console.WriteLine(
            "Erro inesperado ao tentar registrar a oferta no barramento:");
          Console.WriteLine(e);
        }
        // tenta registrar novamente
        Thread.Sleep(waitTime);
      }
    }

    private static bool Login(string entity, byte[] privateKey) {
      try {
        _conn.LoginByCertificate(entity, privateKey);
        return true;
      }
      catch (AlreadyLoggedInException) {
        Console.WriteLine(
          "Falha ao tentar realizar o login por certificado no barramento: a entidade já está com o login realizado. Esta falha será ignorada.");
        return true;
      }
      catch (TRANSIENT) {
        Console.WriteLine("Erro: O barramento não está acessível no momento.");
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
        Console.WriteLine(e);
      }
      catch (Exception e) {
        Console.WriteLine(
          "Erro inesperado ao tentar realizar o login por certificado no barramento:");
        Console.WriteLine(e);
      }
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