using System;
using System.Collections.Generic;
using tecgraf.openbus.core.v2_00.services.access_control;
using tecgraf.openbus.interop.simple;

namespace tecgraf.openbus.interop.multiplexing
{
  /// <summary>
  /// Implementação do servant IHello.
  /// </summary>  
  public class HelloImpl : MarshalByRefObject, Hello
  {
    #region Fields

    private readonly IList<Connection> _conns;

    #endregion

    #region Constructors

    public HelloImpl(IList<Connection> conns)
    {
      _conns = conns;
    }

    #endregion

    #region IHello Members

    public void sayHello()
    {
      try
      {
        foreach (Connection conn in _conns) {
          CallerChain callerChain = conn.CallerChain;
          if (callerChain != null) {
            Console.WriteLine(String.Format("Calling in {0} @ {1}", conn.Login.Value.entity, conn.BusId));
            LoginInfo[] callers = callerChain.Callers;
            String entity = callers[callers.Length - 1].entity;
            Console.WriteLine(String.Format("Hello from {0} @ {1}!", entity, callerChain.BusId));
          }
        }
      }
      catch (Exception e)
      {
        Console.WriteLine(e.StackTrace);
      }
    }

    #endregion
  }
}
