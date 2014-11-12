using System;

namespace tecgraf.openbus.exceptions {
  /// <summary>
  /// Exceção gerada quando tenta-se manipular um fluxo de dados inválido.
  /// </summary>
  [Serializable]
  public sealed class InvalidEncodedStreamException : OpenBusException {
    /// <inheritdoc />
    internal InvalidEncodedStreamException() {
    }

    /// <inheritdoc />
    internal InvalidEncodedStreamException(string message)
      : base(message) {
    }

    /// <inheritdoc />
    internal InvalidEncodedStreamException(string message, Exception inner)
      : base(message, inner) {
    }
  }
}