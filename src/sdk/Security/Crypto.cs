using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using tecgraf.openbus.core.v2_00;

namespace tecgraf.openbus.sdk.Security {
  /// <summary>
  /// Classe responsável pela segurança do OpenBus.
  /// </summary>
  public static class Crypto {
    #region Fields

    private const string CipherAlgorithm = "RSA/ECB/PKCS1Padding";
    private const string SignerAlgorithm = "NONEwithRSA";

    public static readonly Encoding TextEncoding = new ASCIIEncoding();

    #endregion

    #region Members

    /// <summary>
    /// Encripta uma informação utilizando uma chave digital.
    /// </summary>
    /// <param name="key">A chave a ser usada para criptografar os dados.</param>
    /// <param name="data">A informação descriptografada.</param>
    /// <returns>A informação encriptografada.</returns>
    public static byte[] Encrypt(AsymmetricKeyParameter key, byte[] data) {
      IBufferedCipher cipher = CipherUtilities.GetCipher(CipherAlgorithm);
      cipher.Init(true, key);
      return cipher.DoFinal(data);
      /*
      RsaEngine e = new RsaEngine();
      e.Init(true, key);

      int blockSize = e.GetInputBlockSize();

      List<byte> output = new List<byte>();

      for (int chunkPosition = 0;
           chunkPosition < data.Length;
           chunkPosition += blockSize) {
        int chunkSize = Math.Min(blockSize, data.Length -
                                            (chunkPosition * blockSize));
        output.AddRange(e.ProcessBlock(data, chunkPosition,
                                       chunkSize));
      }
      return output.ToArray();
       */
    }

    /// <summary>
    /// Descriptografa uma informação utilizando uma chave digital.
    /// </summary>
    /// <param name="key">A chave a ser usada para descriptografar os dados.</param>
    /// <param name="data">A informação criptografada.</param>
    /// <returns>A informação descriptografada.</returns>
    public static byte[] Decrypt(AsymmetricKeyParameter key, byte[] data) {
      IBufferedCipher cipher = CipherUtilities.GetCipher(CipherAlgorithm);
      cipher.Init(false, key);
      return cipher.DoFinal(data);
      /*
      RsaEngine e = new RsaEngine();
      e.Init(false, key);

      int blockSize = e.GetInputBlockSize();

      List<byte> output = new List<byte>();

      for (int chunkPosition = 0;
           chunkPosition < data.Length;
           chunkPosition += blockSize) {
        int chunkSize = Math.Min(blockSize, data.Length -
                                            (chunkPosition * blockSize));
        output.AddRange(e.ProcessBlock(data, chunkPosition,
                                       chunkSize));
      }
      return output.ToArray();
       */
    }

    public static AsymmetricCipherKeyPair GenerateKeyPair() {
      IAsymmetricCipherKeyPairGenerator kpGen =
        GeneratorUtilities.GetKeyPairGenerator("RSA");
      // EncryptedBlockSize is in bytes but expected parameter is in bits
      kpGen.Init(new KeyGenerationParameters(new SecureRandom(),
                                             EncryptedBlockSize.ConstVal * 8));
      return kpGen.GenerateKeyPair();
    }

    public static AsymmetricKeyParameter CreatePublicKeyFromBytes (byte[] key) {
      return PublicKeyFactory.CreateKey(key);
    }

    public static AsymmetricKeyParameter CreatePrivateKeyFromBytes(byte[] key) {
      return PrivateKeyFactory.CreateKey(key);
    }

    public static byte[] GetPublicKeyInBytes(AsymmetricKeyParameter publicKey) {
      SubjectPublicKeyInfo publicKeyInfo =
        SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(publicKey);
      return publicKeyInfo.ToAsn1Object().GetDerEncoded();
    }

    public static bool VerifySignature(AsymmetricKeyParameter key,
                                       byte[] message, byte[] signature) {
      ISigner signer = SignerUtilities.GetSigner(SignerAlgorithm);
      signer.Init(false, key);
      byte[] hash = SHA256.Create().ComputeHash(message);
      signer.BlockUpdate(hash, 0, hash.Length);
      return signer.VerifySignature(signature);
    }

    #endregion
  }
}