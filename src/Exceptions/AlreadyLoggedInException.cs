using System;

namespace tecgraf.openbus.exceptions
{
  /// <summary>
  /// Indica uma exece��o de falha no servi�o de controle de acesso. 
  /// </summary>
  [Serializable]
  public class AlreadyLoggedInException : OpenBusException
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
