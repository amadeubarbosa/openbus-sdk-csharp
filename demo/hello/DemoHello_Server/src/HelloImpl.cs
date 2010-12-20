using System;
using demoidl.hello;
using OpenbusAPI;
using Scs.Core;
using tecgraf.openbus.core.v1_05.access_control_service;


namespace Server
{
  /// <summary>
  /// Implementação do servant IHello.
  /// </summary>  
  public class HelloImpl : MarshalByRefObject, IHello
  {

    #region Fields

    private ComponentContext context;

    #endregion

    #region Constructors

    public HelloImpl(ComponentContext context) {
      this.context = context;
    }

    #endregion

    #region IHello Members

    public void sayHello() {
      Credential caller = Openbus.GetInstance().GetInterceptedCredential();
      Console.WriteLine(String.Format("Hello {0}!", caller.owner));
    }

    #endregion
  }
}
