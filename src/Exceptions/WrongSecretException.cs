using System;

namespace tecgraf.openbus.exceptions {
  /// <summary>
  /// Indica uma exce��o de falha no servi�o de controle de acesso. 
  /// </summary>
  [Serializable]
  public class WrongSecretException : OpenBusException {
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