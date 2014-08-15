using System;

namespace tecgraf.openbus.exceptions {
  /// <summary>
  /// Indica que tentou-se obter um dado de um ORB OpenBus sem inicializá-lo antes.
  /// </summary>
  [Serializable]
  public sealed class ORBNotInitializedException : OpenBusException {
    /// <inheritdoc />
    internal ORBNotInitializedException() {
    }

    /// <inheritdoc />
    internal ORBNotInitializedException(string message)
      : base(message) {
    }

    /// <inheritdoc />
    internal ORBNotInitializedException(string message, Exception inner)
      : base(message, inner) {
    }
  }
}
