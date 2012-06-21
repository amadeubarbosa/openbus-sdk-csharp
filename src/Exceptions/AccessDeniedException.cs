using System;

namespace tecgraf.openbus.exceptions
{
  /// <summary>
  /// Indica uma execeção de falha no serviço de controle de acesso. 
  /// </summary>
  [Serializable]
  public class AccessDeniedException : OpenBusException
  {
    /// <inheritdoc />
    internal AccessDeniedException() { }
    
    /// <inheritdoc />
    internal AccessDeniedException(string message) : base(message) { }
    
    /// <inheritdoc />
    internal AccessDeniedException(string message, Exception inner) 
      : base(message, inner) { }
  }
}
