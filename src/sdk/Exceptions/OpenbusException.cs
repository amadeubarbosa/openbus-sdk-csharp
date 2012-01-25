using System;

namespace tecgraf.openbus.sdk.Exceptions
{
  /// <summary>
  /// Indica uma exce��o do OpenBus.
  /// </summary>
  [Serializable]
  public class OpenbusException : ApplicationException
  {
    /// <summary>
    /// Cria uma exece��o do OpenBus
    /// </summary>
    public OpenbusException() { }
    
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
    public OpenbusException(string message, Exception inner) 
      : base(message, inner) { }
  }
}
