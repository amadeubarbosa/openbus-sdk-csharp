using System;
using Scs.Core;
using tecgraf.openbus.demo.hello;
using tecgraf.openbus.sdk;
using tecgraf.openbus.sdk.Standard;

namespace Server
{
  /// <summary>
  /// Implementação do servant IHello.
  /// </summary>  
  public class HelloImpl : MarshalByRefObject, IHello
  {
    #region Fields

    private ComponentContext _context;
    private Connection _conn;

    #endregion

    #region Constructors

    public HelloImpl(ComponentContext context, Connection conn) {
      _context = context;
      _conn = conn;
    }

    #endregion

    #region IHello Members

    public void sayHello() {
      Console.WriteLine(String.Format("Hello {0}!", HelloServer.Conn.GetCallerChain().Callers()[0].entity));
    }

    #endregion
  }
}
