using System;
using System.Collections.Generic;
using tecgraf.openbus.interop.simple;

namespace tecgraf.openbus.interop.multiplexing {
  /// <summary>
  /// Implementação do servant Hello.
  /// </summary>  
  public class HelloImpl : MarshalByRefObject, Hello {
    #region Fields

    private readonly IList<Connection> _conns;

    #endregion

    #region Constructors

    internal HelloImpl(IList<Connection> conns) {
      _conns = conns;
    }

    #endregion

    #region Hello Members

    public void sayHello() {
      try {
        foreach (Connection conn in _conns) {
          CallerChain callerChain = conn.CallerChain;
          if (callerChain != null) {
            Console.WriteLine(String.Format("Calling in {0} @ {1}",
                                            conn.Login.Value.entity, conn.BusId));
            String entity = callerChain.Caller.entity;
            Console.WriteLine(String.Format("Hello from {0} @ {1}!", entity,
                                            callerChain.BusId));
          }
        }
      }
      catch (Exception e) {
        Console.WriteLine(e);
      }
    }

    #endregion
  }
}