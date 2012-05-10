using System;

namespace tecgraf.openbus.exceptions
{
  /// <summary>
  /// Indica uma execeção de falha no serviço de controle de acesso. 
  /// </summary>
  [Serializable]
  public class WrongPrivateKeyException : OpenBusException
  {
    /// <inheritdoc />
    public WrongPrivateKeyException() { }
    
    /// <inheritdoc />
    public WrongPrivateKeyException(string message) : base(message) { }
    
    /// <inheritdoc />
    public WrongPrivateKeyException(string message, Exception inner) 
      : base(message, inner) { }
  }
}
