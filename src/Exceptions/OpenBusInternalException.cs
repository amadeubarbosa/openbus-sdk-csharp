using System;

namespace tecgraf.openbus.exceptions {
  /// <summary>
  /// Indica uma exce��o interna do SDK do OpenBus.
  /// </summary>
  [Serializable]
  public sealed class OpenBusInternalException : OpenBusException {
    /// <inheritdoc />
    internal OpenBusInternalException() {
    }

    /// <inheritdoc />
    internal OpenBusInternalException(string message)
      : base(message) {
    }

    /// <inheritdoc />
    internal OpenBusInternalException(string message, Exception inner)
      : base(message, inner) {
    }
  }
}