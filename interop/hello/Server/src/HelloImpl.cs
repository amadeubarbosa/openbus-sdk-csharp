using System;
using log4net;
using tecgraf.openbus.core.v2_1.services.access_control;

namespace tecgraf.openbus.interop.simple {
  /// <summary>
  /// Implementação do servant Hello.
  /// </summary>  
  public class HelloImpl : MarshalByRefObject, Hello {
    private static readonly ILog Logger =
      LogManager.GetLogger(typeof(HelloImpl));

    #region Hello Members

    public string sayHello() {
      try {
        LoginInfo caller = ORBInitializer.Context.CallerChain.Caller;
        string hello = String.Format("Hello {0}!", caller.entity);
        Logger.Info(hello);
        return hello;
      }
      catch (Exception e) {
        Logger.Info(e.StackTrace);
      }
      return "Hello World!";
    }

    #endregion

    public override object InitializeLifetimeService() {
      return null;
    }
  }
}