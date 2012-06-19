using System;

namespace tecgraf.openbus.exceptions
{
  /// <summary>
  /// Indica uma exce��o de falha no servi�o de controle de acesso. 
  /// </summary>
  [Serializable]
  public class WrongSecretException : OpenBusException
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
