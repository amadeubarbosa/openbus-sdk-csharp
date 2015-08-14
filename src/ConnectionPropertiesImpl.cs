using Org.BouncyCastle.Crypto;
using tecgraf.openbus.exceptions;

namespace tecgraf.openbus {
  /// <inheritdoc />
  public class ConnectionPropertiesImpl : ConnectionProperties {
    internal const string LegacyDisableProperty = "legacy.disable";
    internal const bool LegacyDisableDefault = false;
    internal const string AccessKeyProperty = "access.key";
    private AsymmetricCipherKeyPair _accessKey;

    /// <inheritdoc />
    public ConnectionPropertiesImpl() {
      LegacyDisable = LegacyDisableDefault;
    }

    /// <inheritdoc />
    public bool LegacyDisable { get; set; }

    /// <inheritdoc />
    public AsymmetricCipherKeyPair AccessKey {
      get { return _accessKey; }
      set {
        if (value == null) {
          _accessKey = null;
          return;
        }
        if ((value.Private == null) || !value.Private.IsPrivate) {
          throw new InvalidPropertyValueException(AccessKeyProperty,
                                                  "Chave contém apenas informações de chave pública.");
        }
        _accessKey = value;
      }
    }
  }
}