using System;


namespace Demo_Hello
{
  /// <summary>
  /// Implementa��o do servant IHello.
  /// </summary>
  class HelloImpl : demoidl.hello.IHello
  {
    #region IHello Members

    public void sayHello() {
      Console.WriteLine("Hello!");
    }

    #endregion
  }
}
