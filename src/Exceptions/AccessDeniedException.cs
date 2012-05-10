using System;

namespace tecgraf.openbus.exceptions
{
  /// <summary>
  /// Indica uma exece��o de falha no servi�o de controle de acesso. 
  /// </summary>
  [Serializable]
  public class AccessDeniedException : OpenBusException
  {
    /// <inheritdoc />
    public AccessDeniedException() { }
    
    /// <inheritdoc />
    public AccessDeniedException(string message) : base(message) { }
    
    /// <inheritdoc />
    public AccessDeniedException(string message, Exception inner) 
      : base(message, inner) { }
  }
}
