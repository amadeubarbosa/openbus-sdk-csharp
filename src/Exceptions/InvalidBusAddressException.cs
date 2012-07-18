using System;

namespace tecgraf.openbus.exceptions {
  /// <summary>
  /// Indica que o conjunto host e porta fornecido para a conexão a um barramento não pode ser usado para gerar um corbaloc válido ou não referenciam um IComponent do OpenBus 2.0 válido.
  /// </summary>
  [Serializable]
  public sealed class InvalidBusAddressException : OpenBusException {
    /// <inheritdoc />
    internal InvalidBusAddressException() {
    }

    /// <inheritdoc />
    internal InvalidBusAddressException(string message)
      : base(message) {
    }

    /// <inheritdoc />
    internal InvalidBusAddressException(string message, Exception inner)
      : base(message, inner) {
    }
  }
}