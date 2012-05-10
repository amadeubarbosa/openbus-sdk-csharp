using System;

namespace tecgraf.openbus.exceptions
{
  /// <summary>
  /// Indica uma exce��o do OpenBus.
  /// </summary>
  [Serializable]
  public class OpenBusException : ApplicationException
  {
    /// <summary>
    /// Cria uma exece��o do OpenBus
    /// </summary>
    public OpenBusException() { }
    
    /// <summary>
    /// Cria uma exce��o do OpenBus com uma mensagem associada.
    /// </summary>
    /// <param name="message">A mensagem de erro</param>
    public OpenBusException(string message) : base(message) { }

    /// <summary>
    /// Cria uma exce��o do OpenBus com uma mensagem e uma causa associada.
    /// </summary>
    /// <param name="message">A mensagem de erro</param>
    /// <param name="inner">A exce��o associada</param>
    public OpenBusException(string message, Exception inner) 
      : base(message, inner) { }
  }
}
