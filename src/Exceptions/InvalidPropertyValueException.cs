using System;

namespace tecgraf.openbus.exceptions {
  /// <summary>
  /// Uma propriedade informada não é válida.
  /// </summary>
  [Serializable]
  public sealed class InvalidPropertyValueException : OpenBusException {
    /// <summary>
    /// Nome da propriedade que contém o valor inválido.
    /// </summary>
    public string Property { get; internal set; }

    /// <inheritdoc />
    internal InvalidPropertyValueException(string property) {
      Property = property;
    }

    /// <inheritdoc />
    internal InvalidPropertyValueException(string property, string message)
      : base(message) {
      Property = property;
    }

    /// <inheritdoc />
    internal InvalidPropertyValueException(string property, string message,
                                           Exception inner)
      : base(message, inner) {
      Property = property;
    }
  }
}