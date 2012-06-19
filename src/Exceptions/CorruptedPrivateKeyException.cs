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
    public CorruptedPrivateKeyException() { }
    
    /// <inheritdoc />
    public CorruptedPrivateKeyException(string message) : base(message) { }
    
    /// <inheritdoc />
    public CorruptedPrivateKeyException(string message, Exception inner) 
      : base(message, inner) { }
  }
}
