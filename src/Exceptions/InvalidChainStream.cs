using System;

namespace tecgraf.openbus.exceptions {
  /// <summary>
  /// Exceção gerada quando tenta-se manipular uma cadeia ({@link CallerChain}) inválida.
  /// </summary>
  [Serializable]
  public sealed class InvalidChainStreamException : OpenBusException {
    /// <inheritdoc />
    internal InvalidChainStreamException() {
    }

    /// <inheritdoc />
    internal InvalidChainStreamException(string message)
      : base(message) {
    }

    /// <inheritdoc />
    internal InvalidChainStreamException(string message, Exception inner)
      : base(message, inner) {
    }
  }
}