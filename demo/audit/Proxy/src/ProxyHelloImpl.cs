using System;
using tecgraf.openbus;
using tecgraf.openbus.exceptions;

namespace audit {
  /// <summary>
  /// Implementação do servant Hello.
  /// </summary>  
  public class HelloImpl : MarshalByRefObject, Hello {
    #region Fields

    private readonly Connection _conn;
    private readonly Hello _server;

    #endregion

    #region Constructors

    internal HelloImpl(Connection conn, Hello server) {
      _conn = conn;
      _server = server;
    }

    #endregion

    #region Hello Members

    public void sayHello() {
      try {
        CallerChain chain = _conn.CallerChain;
        string caller = chain.Caller.entity;
        Console.WriteLine(
          String.Format(
            "Hello recebido de {0}. Cadeia completa da chamada: {1}", caller,
            ChainToString.ToString(chain)));
        // faz join na própria caller chain
        _conn.JoinChain(null);
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