using System;

namespace tecgraf.openbus.exceptions {
  /// <summary>
  /// Indica que a chave privada est� corrompida ou n�o est� no formato esperado.
  /// </summary>
  [Serializable]
  public class CorruptedPrivateKeyException : OpenBusException {
    /// <inheritdoc />
    internal CorruptedPrivateKeyException() {
    }

    /// <inheritdoc />
    internal CorruptedPrivateKeyException(string message) : base(message) {
    }

    /// <inheritdoc />
    internal CorruptedPrivateKeyException(string message, Exception inner)
      : base(message, inner) {
    }
  }
}