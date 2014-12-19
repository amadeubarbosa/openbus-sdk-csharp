using System;
using System.Reflection;
using System.Text;
using System.Threading;
using Ch.Elca.Iiop.Idl;
using demo.Properties;
using omg.org.CORBA;
using tecgraf.openbus;
using tecgraf.openbus.core.v2_1.services;
using tecgraf.openbus.core.v2_1.services.access_control;
using tecgraf.openbus.core.v2_1.services.offer_registry;

namespace demo {
  /// <summary>
  /// Cliente da demo multiplexing.
  /// </summary>
  internal static class MultiplexingClient {
    private static string _host;
    private static ushort _port;
    private static string _domain;
    private static string _entity;
    private static byte[] _password;
    internal static volatile int Pending;

    private static readonly string TimerIDLType =
      Repository.GetRepositoryID(typeof (Timer));

    private static void Main(String[] args) {
      // Obtém dados através dos argumentos
      _host = args[0];
      _port = Convert.ToUInt16(args[1]);
      _domain = args[2];
      _entity = args[3];
      _password =
        new ASCIIEncoding().GetBytes(args.Length > 4 ? args[4] : _entity);

      // Cria conexão e a define como conexão padrão tanto para entrada como saída.
      ORBInitializer.InitORB();
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
        ServiceProperty[] properties = {prop, autoProp};
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
      catch (Exception e) {
        NO_PERMISSION npe = null;
        if (e is TargetInvocationException) {
          // caso seja uma exceção lançada pelo SDK, será uma NO_PERMISSION
          npe = e.InnerException as NO_PERMISSION;
        }
        if ((npe == null) && (!(e is NO_PERMISSION))) {
          // caso não seja uma NO_PERMISSION não é uma exceção esperada então deixamos passar.
          throw;
        }
        npe = npe ?? e as NO_PERMISSION;
        if (npe.Minor == NoLoginCode.ConstVal) {
          Console.WriteLine(Resources.NoLoginCodeErrorMsg);
        }
        else {
          throw;
        }
      }
      if (offers != null) {
        for (int i = 0; i < offers.Length; i++) {
          // garante espera de no mínimo 5s para que dê tempo do cliente executar
          // todas as chamadas e aumentar o número de notificações esperadas
          StartTheThread(i+5, offers[i], Thread.CurrentThread);
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
      Console.ReadKey();
    }

    private static Connection NewLogin() {
      Connection conn = ORBInitializer.Context.ConnectByAddress(_host, _port);
      try {
        // Faz o login
        conn.LoginByPassword(_entity, _password, _domain);
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
      catch (Exception e) {
        NO_PERMISSION npe = null;
        if (e is TargetInvocationException) {
          // caso seja uma exceção lançada pelo SDK, será uma NO_PERMISSION
          npe = e.InnerException as NO_PERMISSION;
        }
        if ((npe == null) && (!(e is NO_PERMISSION))) {
          // caso não seja uma NO_PERMISSION não é uma exceção esperada então deixamos passar.
          throw;
        }
        npe = npe ?? e as NO_PERMISSION;
        if (npe.Minor == NoLoginCode.ConstVal) {
          Console.WriteLine(Resources.NoLoginCodeErrorMsg);
        }
        else {
          throw;
        }
      }
      return conn;
    }

    private static void StartTheThread(double timeout, ServiceOfferDesc desc,
                                       Thread wThread) {
      Thread t = new Thread(() => UseOffer(timeout, desc, wThread));
      t.Start();
    }

    private static void UseOffer(double timeout, ServiceOfferDesc desc,
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
      catch (Exception e) {
        NO_PERMISSION npe = null;
        if (e is TargetInvocationException) {
          // caso seja uma exceção lançada pelo SDK, será uma NO_PERMISSION
          npe = e.InnerException as NO_PERMISSION;
        }
        if ((npe == null) && (!(e is NO_PERMISSION))) {
          // caso não seja uma NO_PERMISSION não é uma exceção esperada então deixamos passar.
          throw;
        }
        npe = npe ?? e as NO_PERMISSION;
        bool found = false;
        string message = String.Empty;
        switch (npe.Minor) {
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