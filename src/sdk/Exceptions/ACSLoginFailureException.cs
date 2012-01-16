using System;

namespace tecgraf.openbus.sdk.Exceptions
{
  /// <summary>
  /// Indica uma exece��o de falha no servi�o de controle de acesso. 
  /// </summary>
  [Serializable]
  public class ACSLoginFailureException : OpenbusException
  {
    /// <inheritdoc />
    public ACSLoginFailureException() { }
    
    /// <inheritdoc />
    public ACSLoginFailureException(string message) : base(message) { }
    
    /// <inheritdoc />
    public ACSLoginFailureException(string message, System.Exception inner) 
      : base(message, inner) { }
  }
}
