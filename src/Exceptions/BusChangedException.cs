using System;

namespace tecgraf.openbus.exceptions {
  /// <summary>
  /// Indica que o valor do BusId atualmente é diferente do obtido originalmente por uma conexão.
  /// Isso significa que essa conexão deve ser descartada e uma nova criada.
  /// </summary>
  [Serializable]
  public sealed class BusChangedException : OpenBusException {
    /// <inheritdoc />
    internal BusChangedException() {
    }

    /// <inheritdoc />
    internal BusChangedException(string message)
      : base(message) {
    }

    /// <inheritdoc />
    internal BusChangedException(string message, Exception inner)
      : base(message, inner) {
    }
  }
}