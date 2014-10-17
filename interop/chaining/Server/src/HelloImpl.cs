using System;
using tecgraf.openbus.core.v2_1.services.access_control;

namespace tecgraf.openbus.interop.simple {
  /// <summary>
  /// Implementação do servant Hello.
  /// </summary>  
  public class HelloImpl : MarshalByRefObject, Hello {
    #region Hello Members

    public string sayHello() {
      try {
        LoginInfo caller = ORBInitializer.Context.CallerChain.Originators[0];
        string hello = String.Format("Hello {0}!", caller.entity);
        Console.WriteLine(hello);
        return hello;
      }
      catch (Exception e) {
        Console.WriteLine(e.StackTrace);
      }
      return "Hello World!";
    }

    #endregion

    public override object InitializeLifetimeService() {
      return null;
    }
  }
}