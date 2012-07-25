using System;
using tecgraf.openbus;
using tecgraf.openbus.exceptions;

namespace audit {
  /// <summary>
  /// Implementa��o do servant Hello.
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
        CallerChain chain = _dispatcherConn.CallerChain;
        string caller = chain.Caller.entity;
        Console.WriteLine(
          String.Format(
            "Hello recebido de {0}. Cadeia completa da chamada: {1}", caller,
            ChainToString.ToString(chain)));
      }
      catch (OpenBusException e) {
        Console.WriteLine(
          "Erro no m�todo sayHello ao obter a cadeia de chamadas:");
        Console.WriteLine(e);
      }
    }

    #endregion
  }
}