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

    public string sayHello() {
      string ret = String.Empty;
      try {
        foreach (Connection conn in _conns) {
          CallerChain callerChain = conn.CallerChain;
          if (callerChain != null) {
            String entity = callerChain.Caller.entity;
            ret = String.Format("Hello {0}@{1}!", entity,
                                callerChain.BusId);
            Console.WriteLine(ret);
            return ret;
          }
        }
      }
      catch (Exception e) {
        Console.WriteLine(e);
      }
      return ret;
    }

    #endregion
  }
}