using System;

namespace tecgraf.openbus.exceptions {
  /// <summary>
  /// Indica uma exceção de falha no serviço de controle de acesso. 
  /// </summary>
  [Serializable]
  public class AlreadyLoggedInException : OpenBusException {
    /// <inheritdoc />
    internal AlreadyLoggedInException() {
    }

    /// <inheritdoc />
    internal AlreadyLoggedInException(string message) : base(message) {
    }

    /// <inheritdoc />
    internal AlreadyLoggedInException(string message, Exception inner)
      : base(message, inner) {
    }
  }
}