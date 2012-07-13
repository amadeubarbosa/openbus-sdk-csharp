using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using omg.org.CORBA;
using tecgraf.openbus;
using tecgraf.openbus.core.v2_0.services;
using tecgraf.openbus.core.v2_0.services.access_control;
using tecgraf.openbus.core.v2_0.services.offer_registry;
using tecgraf.openbus.exceptions;

namespace Client {
  /// <summary>
  /// Cliente da demo Independent Clock.
  /// </summary>
  internal static class IndependentClockClient {
    private static Connection _conn;
    private static Clock _serverClock;
    private static int _waitTime;
    private static string _host;
    private static short _port;
    private static string _entity;
    private static byte[] _password;
    private static string _serverEntity;

    private static void Main(String[] args) {
      // Obt�m dados atrav�s dos argumentos
      _host = args[0];
      _port = Convert.ToInt16(args[1]);
      _entity = args[2];
      _password = new ASCIIEncoding().GetBytes(args[3]);
      _serverEntity = args[4];
      _waitTime = Convert.ToInt32(args[5]);

      // Inicia thread que tenta conectar ao barramento
      ThreadStart ts = TryLoginAndFindForever;
      Thread t = new Thread(ts) {IsBackground = true};
      t.Start();

      // Realiza trabalho independente do OpenBus
      while (true) {
        if (_serverClock == null) {
          Console.WriteLine(String.Format("Hora local: {0:HH:mm:ss}",
                                          DateTime.Now));
        }
        else {
          try {
            // utiliza o servi�o
            long ticks = _serverClock.getTimeInTicks();
            DateTime serverTime = new DateTime(ticks);
            Console.WriteLine(String.Format("Hora do servidor: {0:HH:mm:ss}",
                                            serverTime));
          }
          catch (Exception) {
            // Se ocorrer algum erro n�o esperado e um processo de conex�o n�o
            // estiver em andamento, tenta refazer o processo de conex�o
            if (t.ThreadState == ThreadState.Stopped) {
              _serverClock = null;
              t.Start();
            }
            continue;
          }
        }
        Thread.Sleep(_waitTime);
      }
    }

    private static void TryLoginAndFindForever() {
      // tenta conectar, fazer login E encontrar ofertas. Se o registro falhar devido � conex�o ou login ter sido perdido, o procedimento deve ser reiniciado.
      bool firstTime = true;
      while (true) {
        // Se n�o for a primeira vez, espera
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
          if (!Connect()) {
            continue;
          }
        }

        // tenta fazer o login
        if (!Login()) {
          continue;
        }

        // Faz busca utilizando propriedades geradas automaticamente e propriedades definidas pelo servi�o espec�fico
        // propriedade gerada automaticamente
        ServiceProperty autoProp = new ServiceProperty("openbus.offer.entity",
                                                       _serverEntity);
        // propriedade definida pelo servi�o clock
        ServiceProperty prop = new ServiceProperty("offer.domain",
                                                   "OpenBus Demos");
        ServiceProperty[] properties = new[] {prop, autoProp};
        ServiceOfferDesc[] offers = Find(properties);

        // analiza as ofertas encontradas
        _serverClock = GetClock(offers);
        if (_serverClock != null) {
          // obteve o servidor clock, termina a thread
          break;
        }
        // se chegou aqui aconteceu algum erro, ent�o volta pro come�o
      }
    }

    private static Clock GetClock(ICollection<ServiceOfferDesc> offers) {
      if (offers.Count < 1) {
        Console.WriteLine("O servi�o Clock n�o se encontra no barramento.");
        return null;
      }

      if (offers.Count > 1) {
        Console.WriteLine(
          "Existe mais de um servi�o Clock no barramento. Tentaremos encontrar uma funcional.");
      }
      foreach (ServiceOfferDesc serviceOfferDesc in offers) {
        Console.WriteLine("Testando uma das ofertas recebidas...");
        try {
          MarshalByRefObject clockObj =
            serviceOfferDesc.service_ref.getFacet(
              "IDL:Clock:1.0");
          if (clockObj == null) {
            Console.WriteLine(
              "N�o foi poss�vel encontrar uma faceta Clock na oferta.");
            continue;
          }
          Clock clock = clockObj as Clock;
          if (clock == null) {
            Console.WriteLine(
              "Faceta encontrada na oferta n�o implementa Clock.");
            continue;
          }
          Console.WriteLine(
            "Foi encontrada uma oferta com um servi�o funcional.");
          return clock;
        }
        catch (TRANSIENT) {
          Console.WriteLine(
            "A oferta � de um cliente inativo. Tentando a pr�xima.");
        }
      }
      Console.WriteLine(
        "N�o foi encontrada uma oferta com um servi�o funcional.");
      return null;
    }

    private static ServiceOfferDesc[] Find(ServiceProperty[] properties) {
      try {
        return _conn.Offers.findServices(properties);
      }
      catch (ServiceFailure e) {
        Console.WriteLine(
          "Erro ao tentar realizar a busca por um servi�o no barramento: Falha no servi�o remoto. Causa:");
        Console.WriteLine(e);
      }
      catch (NO_PERMISSION e) {
        if (e.Minor.Equals(NoLoginCode.ConstVal)) {
          // o login foi perdido, precisa tentar fazer o login novamente
          return null;
        }
      }
      catch (Exception e) {
        Console.WriteLine(
          "Erro inesperado ao tentar realizar a busca por um servi�o no barramento:");
        Console.WriteLine(e);
        _conn = null;
      }
      return null;
    }

    private static bool Login() {
      try {
        _conn.LoginByPassword(_entity, _password);
        return true;
      }
      catch (AlreadyLoggedInException) {
        Console.WriteLine(
          "Falha ao tentar realizar o login por senha no barramento: a entidade j� est� com o login realizado. Esta falha ser� ignorada.");
        return true;
      }
      catch (AccessDenied) {
        Console.WriteLine(
          "Erro ao tentar realizar o login por senha no barramento: a senha fornecida n�o foi validada para a entidade " +
          _entity + ".");
      }
      catch (ServiceFailure e) {
        Console.WriteLine(
          "Erro ao tentar realizar o login por senha no barramento: Falha no servi�o remoto. Causa:");
        Console.WriteLine(e);
      }
      catch (TRANSIENT) {
        Console.WriteLine(
          "O barramento n�o est� acess�vel para realizar o login.");
        _conn = null;
      }
      catch (Exception e) {
        Console.WriteLine(
          "Erro inesperado ao tentar realizar o login por senha no barramento:");
        Console.WriteLine(e);
        _conn = null;
      }
      return false;
    }

    private static bool Connect() {
      // Cria conex�o e a define como conex�o padr�o tanto para entrada como sa�da.
      // O uso exclusivo da conex�o padr�o (sem uso de requester e dispatcher) s� � recomendado para aplica��es que criem apenas uma conex�o e desejem utiliz�-la em todos os casos. Para situa��es diferentes, consulte o manual do SDK OpenBus e/ou outras demos.
      ConnectionManager manager = ORBInitializer.Manager;
      try {
        _conn = manager.CreateConnection(_host, _port);
        manager.DefaultConnection = _conn;
        // Registra uma callback para o caso do login ser perdido
        _conn.OnInvalidLogin = new IndependentClockClientInvalidLoginCallback();
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
  }
}