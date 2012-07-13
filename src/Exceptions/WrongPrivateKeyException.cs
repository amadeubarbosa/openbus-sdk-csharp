using System;

namespace tecgraf.openbus.exceptions {
  /// <summary>
  /// Indica que a chave privada fornecida não era a esperada.
  /// </summary>
  [Serializable]
  public sealed class WrongPrivateKeyException : OpenBusException {
    /// <inheritdoc />
    internal WrongPrivateKeyException() {
    }

    /// <inheritdoc />
    internal WrongPrivateKeyException(string message) : base(message) {
    }

    /// <inheritdoc />
    internal WrongPrivateKeyException(string message, Exception inner)
      : base(message, inner) {
    }
  }
}