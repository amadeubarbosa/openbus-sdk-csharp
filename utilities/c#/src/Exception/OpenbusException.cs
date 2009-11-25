using System;
using System.Collections.Generic;
using System.Text;

namespace OpenbusAPI.Exception
{
  /// <summary>
  /// Indica uma exce��o do OpenBus.
  /// </summary>
  [Serializable()]
  class OpenbusException : System.ApplicationException
  {
    /// <summary>
    /// Cria uma exece��o do Openbus
    /// </summary>
    public OpenbusException() : base() { }


    /// <summary>
    /// Cria uma exce��o do OpenBus com uma mensagem associada.
    /// </summary>
    /// <param name="message">A mensagem de erro</param>
    public OpenbusException(string message) : base(message) { }

    /// <summary>
    /// Cria uma exce��o do OpenBus com uma mensagem e uma causa associada.
    /// </summary>
    /// <param name="message">A mensagem de erro</param>
    /// <param name="inner">A exce��o associada</param>
    public OpenbusException(string message, System.Exception inner) : base(message, inner) { }
  }
}
