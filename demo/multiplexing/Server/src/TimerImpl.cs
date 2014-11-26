using System;
using System.Reflection;
using System.Threading;
using demo.Properties;
using omg.org.CORBA;
using tecgraf.openbus;
using tecgraf.openbus.core.v2_0.services.access_control;

namespace demo {
  /// <summary>
  /// Implementação do servant Timer.
  /// </summary>  
  public class TimerImpl : MarshalByRefObject, Timer {
    #region Timer Members

    public void newTrigger(double timeout, Callback cb) {
      OpenBusContext context = ORBInitializer.Context;
      Connection conn = context.GetCurrentConnection();
      CallerChain chain = context.CallerChain;
      // Inicia thread para realizar a chamada remota na callback
      new Thread(() => NotifyTrigger(conn, chain, timeout, cb)).Start();
    }

    #endregion

    #region Private Members

    private static void NotifyTrigger(Connection current, CallerChain chain,
                                      double timeout, Callback cb) {
      Thread.Sleep(Convert.ToInt32(timeout * 1000));
      OpenBusContext context = ORBInitializer.Context;
      context.SetCurrentConnection(current);
      context.JoinChain(chain);
      try {
        // utiliza o serviço
        cb.notifyTrigger();
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
    }

    #endregion

    public override object InitializeLifetimeService() {
      return null;
    }
  }
}