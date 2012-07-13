using System;

namespace tecgraf.openbus.exceptions {
  /// <summary>
  /// Indica que o segredo fornecido não era o esperado.
  /// </summary>
  [Serializable]
  public sealed class WrongSecretException : OpenBusException {
    /// <inheritdoc />
    internal WrongSecretException() {
    }

    /// <inheritdoc />
    internal WrongSecretException(string message) : base(message) {
    }

    /// <inheritdoc />
    internal WrongSecretException(string message, Exception inner)
      : base(message, inner) {
    }
  }
}