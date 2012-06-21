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

    #endregion

    #region Constructors

    public HelloImpl(Connection conn) {
      _conn = conn;
    }

    #endregion

    #region Hello Members

    public void sayHello() {
      try {
        CallerChain chain = _conn.CallerChain;
        if (chain == null) {
          Console.WriteLine("Hello World!");
          return;
        }
        string caller = chain.Caller.entity;
        Console.WriteLine(
          String.Format(
            "Hello recebido de {0}. Cadeia completa da chamada: {1}", caller,
            ChainToString.ToString(chain)));
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