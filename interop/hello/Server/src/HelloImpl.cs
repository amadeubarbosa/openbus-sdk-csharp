using System;
using tecgraf.openbus.core.v2_0.services.access_control;

namespace tecgraf.openbus.interop.simple {
  /// <summary>
  /// Implementação do servant Hello.
  /// </summary>  
  internal class HelloImpl : MarshalByRefObject, Hello {
    #region Fields

    private readonly Connection _conn;

    #endregion

    #region Constructors

    public HelloImpl(Connection conn) {
      _conn = conn;
    }

    #endregion

    #region Hello Members

    public void sayHello() {
      try {
        LoginInfo caller = _conn.CallerChain.Caller;
        Console.WriteLine(String.Format("Hello {0}!", caller.entity));
      }
      catch (Exception e) {
        Console.WriteLine(e.StackTrace);
      }
    }

    #endregion
  }
}