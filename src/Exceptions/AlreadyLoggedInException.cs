using System;

namespace tecgraf.openbus.exceptions {
  /// <summary>
  /// Indica que a conex�o j� est� autenticada, ao se tentar realizar um login.
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