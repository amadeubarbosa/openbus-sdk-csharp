﻿using tecgraf.openbus.exceptions;
using tecgraf.openbus.security;

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
    private PrivateKeyImpl _accessKey;

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
    public PrivateKey AccessKey {
      get { return _accessKey; }
      set {
        if (value == null) {
          _accessKey = null;
          return;
        }
        PrivateKeyImpl key = value as PrivateKeyImpl;
        if (key == null) {
          throw new InvalidPropertyValueException(AccessKeyProperty,
                                                  "Chave privada não é do tipo esperado pelo SDK, utilize a classe Crypto do SDK para gerar objetos do tipo PrivateKey.");
        }
        if ((key.Pair.Private == null) || !key.Pair.Private.IsPrivate) {
          throw new InvalidPropertyValueException(AccessKeyProperty,
                                                  "Chave contém apenas informações de chave pública.");
        }
        _accessKey = key;
      }
    }
  }
}