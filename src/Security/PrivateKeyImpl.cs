using Org.BouncyCastle.Crypto;

namespace tecgraf.openbus.security {
  /// <summary>
  /// Representação do OpenBus para chaves de acesso.
  /// </summary>
  public class PrivateKeyImpl : PrivateKey {
    internal readonly AsymmetricCipherKeyPair Pair;

    /// <summary>
    /// Gera uma representação do OpenBus para chaves de acesso.
    /// </summary>
    /// <param name="publicKey">Chave pública no formato da biblioteca BouncyCastle.</param>
    /// <param name="privateKey">Chave privada no formato da biblioteca BouncyCastle.</param>
    internal PrivateKeyImpl(AsymmetricKeyParameter publicKey, AsymmetricKeyParameter privateKey) {
      Pair = new AsymmetricCipherKeyPair(publicKey, privateKey);
    }

    /// <summary>
    /// Gera uma representação do OpenBus para chaves de acesso a partir de um par de chaves.
    /// </summary>
    internal PrivateKeyImpl(AsymmetricCipherKeyPair pair) {
      Pair = pair;
    }
  }
}
