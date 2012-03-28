using System;
using demoidl.hello;
using tecgraf.openbus.core.v2_00.services.access_control;
using tecgraf.openbus.sdk;

namespace Server
{
  /// <summary>
  /// Implementação do servant IHello.
  /// </summary>  
  public class HelloImpl : MarshalByRefObject, IHello
  {
    #region Fields

    private readonly Connection _conn;

    #endregion

    #region Constructors

    public HelloImpl(Connection conn) {
      _conn = conn;
    }

    #endregion

    #region IHello Members

    public void sayHello() {
      try {
        //Console.WriteLine("Hello World!");
        //TODO: trocar a linha atual pelas linhas abaixo quando estiver implementado
        LoginInfo[] callers = _conn.GetCallerChain().Callers;
        Console.WriteLine(String.Format("Hello {0}!", callers[0].entity));
      }
      catch (Exception e) {
        Console.WriteLine(e.StackTrace);
      }
    }

    #endregion
  }
}
