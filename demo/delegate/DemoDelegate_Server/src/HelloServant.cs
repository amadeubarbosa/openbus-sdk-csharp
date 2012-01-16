using System;
using Tecgraf.Openbus;
using Scs.Core;
using tecgraf.openbus.core.v1_05.access_control_service;


namespace Server
{
  /// <summary>
  /// Implementação do servant IHello.
  /// </summary>
  public class HelloServant : MarshalByRefObject, demoidl.demoDelegate.IHello
  {
    #region Fields

    private ComponentContext context;

    #endregion

    #region Constructors

    public HelloServant(ComponentContext context) {
      this.context = context;
    }

    #endregion

    #region IHello Members

    public void sayHello(string name) {
      Credential caller = Openbus.GetInstance().GetInterceptedCredential();
      Console.WriteLine(String.Format("[Thread {0}]: Hello {0} !",
          caller._delegate, name));
    }

    #endregion
  }
}
