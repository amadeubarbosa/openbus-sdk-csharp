using System;
using tecgraf.openbus;

namespace demo {
  /// <summary>
  /// Implementação do servant Hello.
  /// </summary>  
  public class HelloImpl : MarshalByRefObject, Hello {
    #region Hello Members

    public void sayHello() {
      Console.WriteLine("Hello {0}!", ORBInitializer.Context.CallerChain.Caller.entity);
    }

    #endregion

    public override object InitializeLifetimeService() {
      return null;
    }
  }
}