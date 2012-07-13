using System;

namespace tecgraf.openbus.exceptions {
  /// <summary>
  /// Indica uma exce��o de falha no servi�o de controle de acesso. 
  /// </summary>
  [Serializable]
  public class WrongPrivateKeyException : OpenBusException {
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