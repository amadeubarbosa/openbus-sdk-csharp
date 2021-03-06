using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using tecgraf.openbus.core.v2_1;
using tecgraf.openbus.exceptions;

namespace tecgraf.openbus.security {
  /// <summary>
  /// Classe responsável pela segurança do OpenBus.
  /// </summary>
  public static class Crypto {
    #region Fields

    private const string CipherAlgorithm = "RSA/ECB/PKCS1Padding";
    private const string SignerAlgorithm = "NONEwithRSA";

    internal static readonly Encoding TextEncoding = new ASCIIEncoding();

    #endregion

    #region Members

    /// <summary>
    /// Gera uma nova chave privada do OpenBus.
    /// </summary>
    /// <returns>A chave privada no formato esperado pelo OpenBus.</returns>
    public static AsymmetricCipherKeyPair NewKey() {
      return GenerateKeyPair();
    }

    /// <summary>
    /// Codifica uma chave privada em bytes no formato esperado pelo OpenBus.
    /// </summary>
    /// <param name="encoded">Chave privada em bytes.</param>
    /// <returns>A chave privada no formato esperado pelo OpenBus.</returns>
    public static AsymmetricCipherKeyPair ReadKey(byte[] encoded) {
      return CreatePairFromBytes(encoded);
    }

    /// <summary>
    /// Codifica uma chave privada lida a partir de um arquivo no formato esperado pelo OpenBus.
    /// </summary>
    /// <param name="filepath">Caminho para o arquivo com a chave privada.</param>
    /// <returns>A chave privada no formato esperado pelo OpenBus.</returns>
    public static AsymmetricCipherKeyPair ReadKeyFile(string filepath) {
      return ReadKey(File.ReadAllBytes(filepath));
    }

    /// <summary>
    /// Codifica uma chave privada do formato nativo .NET no formato esperado pelo OpenBus.
    /// </summary>
    /// <param name="privateKey">Chave privada do formato nativo .NET.</param>
    /// <returns>A chave privada no formato esperado pelo OpenBus.</returns>
    public static AsymmetricCipherKeyPair ReadKey(RSACryptoServiceProvider privateKey) {
      return DotNetUtilities.GetKeyPair(privateKey);
    }

    /// <summary>
    /// Encripta uma informação utilizando uma chave digital.
    /// </summary>
    /// <param name="key">A chave a ser usada para criptografar os dados.</param>
    /// <param name="data">A informação descriptografada.</param>
    /// <returns>A informação encriptografada.</returns>
    internal static byte[] Encrypt(AsymmetricKeyParameter key, byte[] data) {
      IBufferedCipher cipher = CipherUtilities.GetCipher(CipherAlgorithm);
      lock (cipher) {
        cipher.Init(true, key);
        return cipher.DoFinal(data);
      }
    }

    /// <summary>
    /// Descriptografa uma informação utilizando uma chave digital.
    /// </summary>
    /// <param name="key">A chave a ser usada para descriptografar os dados.</param>
    /// <param name="data">A informação criptografada.</param>
    /// <returns>A informação descriptografada.</returns>
    internal static byte[] Decrypt(AsymmetricKeyParameter key, byte[] data) {
      IBufferedCipher cipher = CipherUtilities.GetCipher(CipherAlgorithm);
      lock (cipher) {
        cipher.Init(false, key);
        return cipher.DoFinal(data);
      }
    }

    internal static AsymmetricCipherKeyPair GenerateKeyPair() {
      IAsymmetricCipherKeyPairGenerator kpGen =
        GeneratorUtilities.GetKeyPairGenerator("RSA");
      lock (kpGen) {
        // EncryptedBlockSize is in bytes but expected parameter is in bits
        kpGen.Init(new KeyGenerationParameters(new SecureRandom(),
                                               EncryptedBlockSize.ConstVal * 8));
        return kpGen.GenerateKeyPair();
      }
    }

    private static AsymmetricCipherKeyPair CreatePairFromBytes(byte[] encoded) {
      AsymmetricKeyParameter priv = CreatePrivateKeyFromBytes(encoded);
      RSAParameters rsaParameters = DotNetUtilities.ToRSAParameters((RsaPrivateCrtKeyParameters)priv);
      return DotNetUtilities.GetRsaKeyPair(rsaParameters);
    }

    internal static AsymmetricKeyParameter CreatePublicKeyFromBytes(byte[] key) {
      return PublicKeyFactory.CreateKey(key);
    }

    internal static AsymmetricKeyParameter CreatePublicKeyFromCertificateBytes(byte[] certificate) {
      X509CertificateParser parser = new X509CertificateParser();
      X509Certificate cert = parser.ReadCertificate(certificate);
      cert.CheckValidity();
      return cert.GetPublicKey();
    }

    private static AsymmetricKeyParameter CreatePrivateKeyFromBytes(byte[] key) {
      AsymmetricKeyParameter k;
      try {
        k = PrivateKeyFactory.CreateKey(key);
      }
      catch (NullReferenceException e) {
        throw new InvalidPrivateKeyException(e.Message, e);
      }
      catch (ArgumentException e) {
        throw new InvalidPrivateKeyException(e.Message, e);
      }
      return k;
    }

    internal static byte[] GetPublicKeyInBytes(AsymmetricKeyParameter publicKey) {
      SubjectPublicKeyInfo publicKeyInfo =
        SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(publicKey);
      return publicKeyInfo.ToAsn1Object().GetDerEncoded();
    }

    internal static bool VerifySignature(AsymmetricKeyParameter key,
                                         byte[] message, byte[] signature) {
      ISigner signer = SignerUtilities.GetSigner(SignerAlgorithm);
      lock (signer) {
        signer.Init(false, key);
        byte[] hash = SHA256.Create().ComputeHash(message);
        signer.BlockUpdate(hash, 0, hash.Length);
        return signer.VerifySignature(signature);
      }
    }

    #endregion
  }
}