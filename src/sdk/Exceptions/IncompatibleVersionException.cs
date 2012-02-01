using System;

namespace tecgraf.openbus.sdk.Exceptions {
  /// <summary>
  /// Indica uma exceção de comunicação com um objeto de versão incompatível.
  /// </summary>
  [Serializable]
  public class IncompatibleVersionException : OpenbusException {
    /// <inheritdoc />
    public IncompatibleVersionException() { }

    /// <inheritdoc />
    public IncompatibleVersionException(string message) : base(message) { }

    /// <inheritdoc />
    public IncompatibleVersionException(string message, Exception inner)
      : base(message, inner) { }
  }
}
