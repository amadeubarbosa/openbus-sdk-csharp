using tecgraf.openbus.exceptions;
using tecgraf.openbus.security;

namespace tecgraf.openbus {
  /// <inheritdoc />
  public class ConnectionPropertiesImpl : ConnectionProperties {
    internal const string AccessKeyProperty = "access.key";
    private PrivateKeyImpl _accessKey;

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