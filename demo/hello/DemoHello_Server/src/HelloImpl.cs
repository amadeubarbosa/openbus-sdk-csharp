using System;
using tecgraf.openbus.core.v2_00.services.access_control;
using tecgraf.openbus.sdk;

namespace tecgraf.openbus.demo.hello
{
  /// <summary>
  /// Implementação do servant IHello.
  /// </summary>  
  public class HelloImpl : MarshalByRefObject, Hello
  {
    #region Fields

    private readonly Connection _conn;

    #endregion

    #region Constructors

    public HelloImpl(Connection conn) {
      _conn = conn;
    }

    #endregion

    #region IHello Members

    public void sayHello() {
      try {
        LoginInfo[] callers = _conn.CallerChain.Callers;
        Console.WriteLine(String.Format("Hello {0}!", callers[0].entity));
      }
      catch (Exception e) {
        Console.WriteLine(e.StackTrace);
      }
    }

    #endregion
  }
}
