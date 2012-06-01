using System;
using tecgraf.openbus;
using tecgraf.openbus.core.v2_00.services.access_control;
using tecgraf.openbus.exceptions;

namespace hello
{
  /// <summary>
  /// Implementação do servant Hello.
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

    #region Hello Members

    public void sayHello() {
      try {
        CallerChain chain = _conn.CallerChain;
        if (chain == null) {
          Console.WriteLine("A cadeia de chamadas é nula, talvez o serviço não esteja logado no barramento. Impossível descobrir quem fez a chamada.");
          Console.WriteLine("Hello World!");
          return;
        }
        LoginInfo[] callers = chain.Callers;
        Console.WriteLine(String.Format("Hello {0}!", callers[0].entity));
      }
      catch (OpenBusException e) {
        Console.WriteLine("Erro no método sayHello ao obter a cadeia de chamadas:");
        Console.WriteLine(e);
      }
    }
    #endregion
  }
}
