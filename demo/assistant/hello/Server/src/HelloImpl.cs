using System;
using tecgraf.openbus;

namespace demo {
  /// <summary>
  /// Implementa��o do servant Hello.
  /// </summary>  
  public class HelloImpl : MarshalByRefObject, Hello {
    #region Hello Members

    public void sayHello() {
      Console.WriteLine(String.Format("Hello {0}!",
                                      ORBInitializer.Context.CallerChain.Caller.
                                        entity));
    }

    #endregion
  }
}