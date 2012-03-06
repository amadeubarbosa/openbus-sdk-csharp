using System;

namespace tecgraf.openbus.sdk.exceptions
{
  /// <summary>
  /// Indica uma execeção de falha no serviço de controle de acesso. 
  /// </summary>
  [Serializable]
  public class AlreadyLoggedInException : OpenbusException
  {
    /// <inheritdoc />
    public AlreadyLoggedInException() { }
    
    /// <inheritdoc />
    public AlreadyLoggedInException(string message) : base(message) { }
    
    /// <inheritdoc />
    public AlreadyLoggedInException(string message, Exception inner) 
      : base(message, inner) { }
  }
}
