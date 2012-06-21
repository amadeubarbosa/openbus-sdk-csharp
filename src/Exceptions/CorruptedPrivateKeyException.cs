using System;

namespace tecgraf.openbus.exceptions
{
  /// <summary>
  /// Indica uma exce��o de falha no servi�o de controle de acesso. 
  /// </summary>
  [Serializable]
  public class CorruptedPrivateKeyException : OpenBusException
  {
    /// <inheritdoc />
    internal CorruptedPrivateKeyException() { }
    
    /// <inheritdoc />
    internal CorruptedPrivateKeyException(string message) : base(message) { }
    
    /// <inheritdoc />
    internal CorruptedPrivateKeyException(string message, Exception inner) 
      : base(message, inner) { }
  }
}
