using Org.BouncyCastle.Crypto;

namespace tecgraf.openbus.security {
  /// <summary>
  /// Representação do OpenBus para chaves de acesso.
  /// </summary>
  public class PrivateKey {
    internal readonly AsymmetricKeyParameter PrivKey;

    /// <summary>
    /// Gera uma representação do OpenBus para chaves de acesso.
    /// </summary>
    /// <param name="privateKey">Chave privada no formato da biblioteca BouncyCastle.</param>
    internal PrivateKey(AsymmetricKeyParameter privateKey) {
      PrivKey = privateKey;
    }

    /// <summary>
    /// Gera uma representação do OpenBus para chaves de acesso a partir de um par de chaves.
    /// </summary>
    internal PrivateKey(AsymmetricCipherKeyPair pair) {
      PrivKey = pair.Private;
    }
  }
}
