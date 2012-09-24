using System;
using System.Text;
using System.Threading;
using Ch.Elca.Iiop.Idl;
using demo.Properties;
using omg.org.CORBA;
using tecgraf.openbus;
using tecgraf.openbus.core.v2_0.services;
using tecgraf.openbus.core.v2_0.services.access_control;
using tecgraf.openbus.core.v2_0.services.offer_registry;

namespace demo {
  /// <summary>
  /// Cliente da demo multiplexing.
  /// </summary>
  internal static class MultiplexingClient {
    private static string _host;
    private static ushort _port;
    private static string _entity;
    private static byte[] _password;
    internal static volatile int Pending;

    private static readonly string TimerIDLType =
      Repository.GetRepositoryID(typeof (Timer));

    private static void Main(String[] args) {
      // Obtém dados através dos argumentos
      _host = args[0];
      _port = Convert.ToUInt16(args[1]);
      _entity = args[2];
      _password =
        new ASCIIEncoding().GetBytes(args.Length > 3 ? args[3] : _entity);

      // Cria conexão e a define como conexão padrão tanto para entrada como saída.
      OpenBusContext context = ORBInitializer.Context;
      context.SetDefaultConnection(NewLogin());

      ServiceOfferDesc[] offers = null;
      try {
        // Faz busca utilizando propriedades geradas automaticamente e propriedades definidas pelo serviço específico
        // propriedade gerada automaticamente
        ServiceProperty autoProp =
          new ServiceProperty("openbus.component.interface", TimerIDLType);
        // propriedade definida pelo serviço timer
        ServiceProperty prop = new ServiceProperty("offer.domain",
                                                   "Demo Multiplexing");
        ServiceProperty[] properties = new[] {prop, autoProp};
        offers = context.OfferRegistry.findServices(properties);
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
      if (offers != null) {
        for (int i = 0; i < offers.Length; i++) {
          StartTheThread(i, offers[i], Thread.CurrentThread);
        }
      }

      // Mantém a thread ativa para aguardar requisições
      try {
        Thread.Sleep(Timeout.Infinite);
      }
      catch (ThreadInterruptedException) {
        // Se a thread for acordada, é porque não há mais requisições pendentes
        Console.WriteLine(Resources.ClientOK);
      }

      // Faz logout da conexão usada nessa thread
      try {
        context.GetDefaultConnection().Logout();
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
      Console.ReadLine();
    }

    private static Connection NewLogin() {
      Connection conn = ORBInitializer.Context.CreateConnection(_host, _port,
                                                                null);
      try {
        // Faz o login
        conn.LoginByPassword(_entity, _password);
      }
      catch (AccessDenied) {
        Console.WriteLine(Resources.ClientAccessDenied + _entity + ".");
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
      return conn;
    }

    private static void StartTheThread(int timeout, ServiceOfferDesc desc,
                                       Thread wThread) {
      var t = new Thread(() => UseOffer(timeout, desc, wThread));
      t.Start();
    }

    private static void UseOffer(int timeout, ServiceOfferDesc desc,
                                 Thread wThread) {
      Connection conn = NewLogin();
      if (!conn.Login.HasValue) {
        return;
      }
      OpenBusContext context = ORBInitializer.Context;
      context.SetCurrentConnection(conn);
      bool failed = true;
      try {
        MarshalByRefObject timerObj = desc.service_ref.getFacet(TimerIDLType);
        if (timerObj == null) {
          Console.WriteLine(Resources.FacetNotFoundInOffer);
          return;
        }
        Timer timer = timerObj as Timer;
        if (timer == null) {
          Console.WriteLine(Resources.FacetFoundWrongType);
          return;
        }
        Console.WriteLine(Resources.OfferFound);
        // utiliza o serviço
        timer.newTrigger(timeout,
                         new CallbackImpl(conn.Login.Value.id, desc, wThread));
        failed = false;
      }
      catch (TRANSIENT) {
        Console.WriteLine(Resources.ServiceTransientErrorMsg);
      }
      catch (COMM_FAILURE) {
        Console.WriteLine(Resources.ServiceCommFailureErrorMsg);
      }
      catch (NO_PERMISSION e) {
        bool found = false;
        string message = String.Empty;
        switch (e.Minor) {
          case NoLoginCode.ConstVal:
            message = Resources.NoLoginCodeErrorMsg;
            found = true;
            break;
          case UnknownBusCode.ConstVal:
            message = Resources.UnknownBusCodeErrorMsg;
            found = true;
            break;
          case UnverifiedLoginCode.ConstVal:
            message = Resources.UnverifiedLoginCodeErrorMsg;
            found = true;
            break;
          case InvalidRemoteCode.ConstVal:
            message = Resources.InvalidRemoteCodeErrorMsg;
            found = true;
            break;
        }
        if (found) {
          Console.WriteLine(message);
        }
        else {
          throw;
        }
      }
      finally {
        try {
          context.SetCurrentConnection(null);
          conn.Logout();
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
        if (!failed) {
          Pending++;
        }
      }
    }
  }
}