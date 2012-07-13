using System;
using tecgraf.openbus;
using tecgraf.openbus.exceptions;

namespace hello {
  /// <summary>
  /// Implementa��o do servant Hello.
  /// </summary>  
  internal class HelloImpl : MarshalByRefObject, Hello {
    #region Fields

    private readonly Connection _conn;

    #endregion

    #region Constructors

    internal HelloImpl(Connection conn) {
      _conn = conn;
    }

    #endregion

    #region Hello Members

    public void sayHello() {
      try {
        CallerChain chain = _conn.CallerChain;
        if (chain == null) {
          Console.WriteLine(
            "A cadeia de chamadas � nula, talvez o servi�o n�o esteja logado no barramento. Imposs�vel descobrir quem fez a chamada.");
          Console.WriteLine("Hello World!");
          return;
        }
        Console.WriteLine(String.Format("Hello {0}!", chain.Caller.entity));
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