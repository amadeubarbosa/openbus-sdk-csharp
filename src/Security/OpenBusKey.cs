using Org.BouncyCastle.Crypto;

namespace tecgraf.openbus.security {
  /// <summary>
  /// Representação do OpenBus para chaves de acesso.
  /// </summary>
  public class OpenBusPrivateKey {
    internal readonly AsymmetricKeyParameter _privateKey;

    /// <summary>
    /// Gera uma representação do OpenBus para chaves de acesso.
    /// </summary>
    /// <param name="privateKey">Chave privada no formato da biblioteca BouncyCastle.</param>
    internal OpenBusPrivateKey(AsymmetricKeyParameter privateKey) {
      _privateKey = privateKey;
    }

    /// <summary>
    /// Gera uma representação do OpenBus para chaves de acesso a partir de um par de chaves.
    /// </summary>
    internal OpenBusPrivateKey(AsymmetricCipherKeyPair pair) {
      _privateKey = pair.Private;
    }
  }
}
