using System;

namespace tecgraf.openbus.sdk.Exceptions
{
  /// <summary>
  /// Indica uma exce��o de conex�o com login j� realizado.
  /// </summary>
  [Serializable]
  public class ConnectionAlreadyLoggedIn : OpenbusException
  {
    /// <inheritdoc />
    public ConnectionAlreadyLoggedIn() { }

    /// <inheritdoc />
    public ConnectionAlreadyLoggedIn(string message) : base(message) { }

    /// <inheritdoc />
    public ConnectionAlreadyLoggedIn(string message, Exception inner) 
      : base(message, inner) { }
  }
}
