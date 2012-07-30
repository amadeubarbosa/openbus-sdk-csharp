using System;
using tecgraf.openbus.core.v2_0.services.access_control;

namespace tecgraf.openbus.interop.simple {
  /// <summary>
  /// Implementação do servant Hello.
  /// </summary>  
  public class HelloImpl : MarshalByRefObject, Hello {
    #region Fields

    private readonly Connection _conn;

    #endregion

    #region Constructors

    public HelloImpl(Connection conn) {
      _conn = conn;
    }

    #endregion

    #region Hello Members

    public string sayHello() {
      try {
        LoginInfo caller = _conn.CallerChain.Caller;
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
  }
}