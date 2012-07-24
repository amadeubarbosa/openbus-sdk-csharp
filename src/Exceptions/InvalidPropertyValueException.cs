using System;

namespace tecgraf.openbus.exceptions {
  /// <summary>
  /// Uma propriedade informada não é válida.
  /// </summary>
  [Serializable]
  public sealed class InvalidPropertyValueException : OpenBusException {
    public string Property { get; internal set; }
    public string Value { get; internal set; }

    /// <inheritdoc />
    internal InvalidPropertyValueException(string property, string value) {
      Property = property;
      Value = value;
    }

    /// <inheritdoc />
    internal InvalidPropertyValueException(string property, string value,
                                           string message)
      : base(message) {
      Property = property;
      Value = value;
    }

    /// <inheritdoc />
    internal InvalidPropertyValueException(string property, string value,
                                           string message, Exception inner)
      : base(message, inner) {
      Property = property;
      Value = value;
    }
  }
}