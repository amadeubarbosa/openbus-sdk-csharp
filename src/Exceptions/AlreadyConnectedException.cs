using System;

namespace tecgraf.openbus.exceptions {
  /// <summary>
  /// Indica uma exceção de falha no serviço de controle de acesso. 
  /// </summary>
  [Serializable]
  public class AlreadyConnectedException : OpenBusException {
    /// <inheritdoc />
    public AlreadyConnectedException() { }

    /// <inheritdoc />
    public AlreadyConnectedException(string message) : base(message) { }

    /// <inheritdoc />
    public AlreadyConnectedException(string message, Exception inner)
      : base(message, inner) { }
  }
}
