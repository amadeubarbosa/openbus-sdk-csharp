using System;
using tecgraf.openbus;
using tecgraf.openbus.exceptions;

namespace hello {
  /// <summary>
  /// Implementação do servant Hello.
  /// </summary>  
  public class HelloImpl : MarshalByRefObject, Hello {
    #region Fields

    private readonly Connection _dispatcherConn;

    #endregion

    #region Constructors

    internal HelloImpl(Connection dispatcherConn) {
      _dispatcherConn = dispatcherConn;
    }

    #endregion

    #region Hello Members

    public void sayHello() {
      try {
        Console.WriteLine(String.Format("Hello {0}!",
                                        _dispatcherConn.CallerChain.Caller.
                                          entity));
      }
      catch (OpenBusException e) {
        Console.WriteLine(
          "Erro no método sayHello ao obter a cadeia de chamadas:");
        Console.WriteLine(e);
      }
    }

    #endregion
  }
}