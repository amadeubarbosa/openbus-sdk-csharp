using Org.BouncyCastle.Crypto;
using tecgraf.openbus.exceptions;

namespace tecgraf.openbus {
  /// <inheritdoc />
  public class ConnectionPropertiesImpl : ConnectionProperties {
    internal const string LegacyDisableProperty = "legacy.disable";
    internal const bool LegacyDisableDefault = false;
    internal const string LegacyDelegateProperty = "legacy.delegate";
    private const string LegacyDelegateDefault = "caller";
    internal const string LegacyDelegateOriginatorOption = "originator";
    internal const string AccessKeyProperty = "access.key";

    private string _delegate;
    private AsymmetricCipherKeyPair _accessKey;

    /// <inheritdoc />
    public ConnectionPropertiesImpl() {
      LegacyDisable = LegacyDisableDefault;
      _delegate = LegacyDelegateDefault;
    }

    /// <inheritdoc />
    public bool LegacyDisable { get; set; }

    /// <inheritdoc />
    public string LegacyDelegate {
      get { return _delegate; }
      set {
        string deleg = value;
        string temp = deleg.ToLower();
        switch (temp) {
          case LegacyDelegateOriginatorOption:
            _delegate = LegacyDelegateOriginatorOption;
            break;
          case LegacyDelegateDefault:
            break;
          default:
            throw new InvalidPropertyValueException(LegacyDelegateProperty,
                                                    deleg);
        }
      }
    }

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