using System;

namespace tecgraf.openbus.exceptions {
  /// <summary>
  /// O LoginProcess informado é inválido, por exemplo depois de ser cancelado ou ter expirado.
  /// </summary>
  [Serializable]
  public class InvalidLoginProcessException : OpenBusException {
    /// <inheritdoc />
    public InvalidLoginProcessException() { }

    /// <inheritdoc />
    public InvalidLoginProcessException(string message) : base(message) { }

    /// <inheritdoc />
    public InvalidLoginProcessException(string message, Exception inner)
      : base(message, inner) { }
  }
}
