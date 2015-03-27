using System;
using log4net;
using tecgraf.openbus.interop.simple;

namespace tecgraf.openbus.interop.multiplexing {
  /// <summary>
  /// Implementação do servant Hello.
  /// </summary>  
  public class HelloImpl : MarshalByRefObject, Hello {
    private static readonly ILog Logger =
      LogManager.GetLogger(typeof(HelloImpl));

    #region Hello Members

    public string sayHello() {
      string ret = String.Empty;
      try {
        CallerChain callerChain = ORBInitializer.Context.CallerChain;
        if (callerChain != null) {
          String entity = callerChain.Caller.entity;
          ret = String.Format("Hello {0}@{1}!", entity,
                              callerChain.BusId);
          Logger.Info(ret);
          return ret;
        }
      }
      catch (Exception e) {
        Logger.Info(e);
      }
      return ret;
    }

    #endregion

    public override object InitializeLifetimeService() {
      return null;
    }
  }
}