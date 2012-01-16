using System;

namespace tecgraf.openbus.sdk.Exceptions
{
  /// <summary>
  /// Indica uma exce��o de servi�o de controle de acesso indispon�vel
  /// </summary>
  [Serializable]
  public class ACSUnavailableException : OpenbusException
  {
    /// <inheritdoc />
    public ACSUnavailableException() { }

    /// <inheritdoc />
    public ACSUnavailableException(string message) : base(message) { }

    /// <inheritdoc />
    public ACSUnavailableException(string message, System.Exception inner) 
      : base(message, inner) { }
  }
}
