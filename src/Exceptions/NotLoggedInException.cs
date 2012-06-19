using System;

namespace tecgraf.openbus.exceptions
{
  /// <summary>
  /// Indica uma exceção de falha devido à aplicação não estar autenticada no barramento. 
  /// </summary>
  [Serializable]
  public class NotLoggedInException : OpenBusException
  {
    /// <inheritdoc />
    public NotLoggedInException() { }
    
    /// <inheritdoc />
    public NotLoggedInException(string message) : base(message) { }
    
    /// <inheritdoc />
    public NotLoggedInException(string message, Exception inner) 
      : base(message, inner) { }
  }
}
