using System;
using demoidl.demoDelegate;


namespace Demo_Delegate
{
  /// <summary>
  /// Implementa��o do servant IHello.
  /// </summary>
  class HelloImpl : demoidl.demoDelegate.IHello
  {
    #region IHello Members

    void IHello.sayHello(string name) {
      throw new NotImplementedException();
    }

    #endregion
  }
}
