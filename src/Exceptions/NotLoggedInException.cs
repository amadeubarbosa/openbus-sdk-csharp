using System;

namespace tecgraf.openbus.exceptions {
  /// <summary>
  /// Indica uma falha devido � aplica��o n�o estar autenticada no barramento.
  /// </summary>
  [Serializable]
  public class NotLoggedInException : OpenBusException {
    /// <inheritdoc />
    internal NotLoggedInException() {
    }

    /// <inheritdoc />
    internal NotLoggedInException(string message) : base(message) {
    }

    /// <inheritdoc />
    internal NotLoggedInException(string message, Exception inner)
      : base(message, inner) {
    }
  }
}