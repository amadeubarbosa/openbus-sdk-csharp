using System;

namespace tecgraf.openbus.exceptions {
  /// <summary>
  /// Indica uma exceção do SDK do OpenBus.
  /// </summary>
  [Serializable]
  public abstract class OpenBusException : ApplicationException {
    /// <summary>
    /// Cria uma exceção do OpenBus
    /// </summary>
    internal OpenBusException() {
    }

    /// <summary>
    /// Cria uma exceção do OpenBus com uma mensagem associada.
    /// </summary>
    /// <param name="message">A mensagem de erro</param>
    internal OpenBusException(string message) : base(message) {
    }

    /// <summary>
    /// Cria uma exceção do OpenBus com uma mensagem e uma causa associada.
    /// </summary>
    /// <param name="message">A mensagem de erro</param>
    /// <param name="inner">A exceção associada</param>
    internal OpenBusException(string message, Exception inner)
      : base(message, inner) {
    }
  }
}