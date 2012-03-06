using System;

namespace tecgraf.openbus.sdk.exceptions
{
  /// <summary>
  /// Indica uma exece��o de falha no servi�o de controle de acesso. 
  /// </summary>
  [Serializable]
  public class WrongSecretException : OpenbusException
  {
    /// <inheritdoc />
    public WrongSecretException() { }
    
    /// <inheritdoc />
    public WrongSecretException(string message) : base(message) { }
    
    /// <inheritdoc />
    public WrongSecretException(string message, Exception inner) 
      : base(message, inner) { }
  }
}
