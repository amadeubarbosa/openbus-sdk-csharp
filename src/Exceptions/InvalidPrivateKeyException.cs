using System;

namespace tecgraf.openbus.exceptions {
  /// <summary>
  /// Indica que a chave privada est� corrompida ou n�o est� no formato esperado.
  /// </summary>
  [Serializable]
  public sealed class InvalidPrivateKeyException : OpenBusException {
    /// <inheritdoc />
    internal InvalidPrivateKeyException() {
    }

    /// <inheritdoc />
    internal InvalidPrivateKeyException(string message) : base(message) {
    }

    /// <inheritdoc />
    internal InvalidPrivateKeyException(string message, Exception inner)
      : base(message, inner) {
    }
  }
}