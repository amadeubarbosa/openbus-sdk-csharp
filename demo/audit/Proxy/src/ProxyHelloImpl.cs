using System;
using tecgraf.openbus;
using tecgraf.openbus.exceptions;

namespace audit {
  /// <summary>
  /// Implementação do servant Hello.
  /// </summary>  
  public class HelloImpl : MarshalByRefObject, Hello {
    #region Fields

    private readonly Connection _dispatcherConn;
    private readonly Hello _server;

    #endregion

    #region Constructors

    internal HelloImpl(Connection dispatcherConn, Hello server) {
      _dispatcherConn = dispatcherConn;
      _server = server;
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
        // faz join na própria caller chain
        _dispatcherConn.JoinChain(null);
        _server.sayHello();
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