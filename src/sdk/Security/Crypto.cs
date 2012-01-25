using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using log4net;

namespace tecgraf.openbus.sdk.Security {
  /// <summary>
  /// Classe responsável pela segurança do OpenBus.
  /// </summary>
  public static class Crypto {
    #region Fields

    private static readonly ILog Logger = LogManager.GetLogger(typeof (Crypto));

    #endregion

    #region Members

    public static RSACryptoServiceProvider NewKey() {
      return new RSACryptoServiceProvider(RSAPKCS8KeyFormatter.GetKeyLenght()) {PersistKeyInCsp = false};
    }

    public static X509Certificate2 NewCertificate() {
      return new X509Certificate2();
    }

    public static RSACryptoServiceProvider GetPrivateKey(X509Certificate2 cert) {
      RSACryptoServiceProvider key = cert.PrivateKey as RSACryptoServiceProvider;
      key.KeySize = RSAPKCS8KeyFormatter.GetKeyLenght();
      key.PersistKeyInCsp = false;
      return key;
    }

    public static RSACryptoServiceProvider GetPublicKey(X509Certificate2 cert) {
      return cert.PublicKey.Key as RSACryptoServiceProvider;
    }

    /// <summary>
    /// Lê uma chave privada a partir de um arquivo XML.
    /// </summary>
    /// <param name="privateKeyFileName">O arquivo XML.</param>
    /// <returns>Uma instância de RSACryptoServiceProvider.</returns>
    public static RSACryptoServiceProvider ReadPrivateKey(
      String privateKeyFileName) {
      StreamReader key = File.OpenText(privateKeyFileName);
      String pemstr = key.ReadToEnd().Trim();
      key.Close();

      RSACryptoServiceProvider rsa = RSAPKCS8KeyFormatter.DecodePEMKey(pemstr);
      if (rsa == null) {
        Logger.Fatal("Não foi possível gerar um RSACryptoServiceProvider");
      }

      return rsa;
    }

    /// <summary>
    /// Lê uma chave privada a partir de um array de bytes.
    /// </summary>
    /// <param name="privateKey">O array de bytes.</param>
    /// <returns>Uma instância de RSACryptoServiceProvider.</returns>
    public static RSACryptoServiceProvider ReadPrivateKey(Byte[] privateKey) {
      RSACryptoServiceProvider rsa =
        RSAPKCS8KeyFormatter.DecodePrivateKey(privateKey);
      if (rsa == null) {
        Logger.Fatal("Não foi possível gerar um RSACryptoServiceProvider");
      }
      return rsa;
    }

    /// <summary>
    /// Lê um certificado digital a partir de um arquivo.
    /// </summary>
    /// <param name="certificateFile">O arquivo.</param>
    /// <returns>O certificado formatado em X509.</returns>
    /// <exception cref="System.Security.Cryptography.CryptographicException">
    /// Caso o arquivo não exista, esteja incorreto ou inválido.</exception>
    public static X509Certificate2 ReadCertificate(String certificateFile) {
      return new X509Certificate2(certificateFile);
    }

    /// <summary>
    /// Gera a resposta para o desafio enviado pelo serviço de controle 
    /// de acesso.
    /// </summary>
    /// <param name="challenge">O desafio do serviço de controle de acesso.
    /// </param>
    /// <param name="privateKey">A chave privada originária de um arquivo 
    /// XML.</param>
    /// <param name="acsCertificate">O certificado formatado em X509.</param>
    /// <returns>A resposta do desafio.</returns>
    /// <exception cref="CryptographicException">As chaves não estão corretas.
    /// </exception>
    public static byte[] GenerateAnswer(byte[] challenge,
                                        RSACryptoServiceProvider privateKey,
                                        X509Certificate2 acsCertificate) {
      byte[] plainChallenge = Decrypt(privateKey, challenge);
      return Encrypt(acsCertificate, plainChallenge);
    }

    /// <summary>
    /// Encripta uma informação utilizando um certificado digital.
    /// </summary>
    /// <param name="acsCertificate">O certificado Digital.</param>
    /// <param name="plainData">A informação descriptografada.</param>
    /// <returns>A informação encriptografada.</returns>
    public static byte[] Encrypt(X509Certificate2 acsCertificate,
                                  byte[] plainData) {
      RSACryptoServiceProvider rsaProvider = acsCertificate.PublicKey.Key as
                        RSACryptoServiceProvider;
      return rsaProvider.Encrypt(plainData, false);
    }

    /// <summary>
    /// Encripta uma informação utilizando um certificado digital.
    /// </summary>
    /// <param name="acsKey">A chave a ser usada para criptografar os dados.</param>
    /// <param name="plainData">A informação descriptografada.</param>
    /// <returns>A informação encriptografada.</returns>
    public static byte[] Encrypt(RSACryptoServiceProvider acsKey,
                                  byte[] plainData) {
      return acsKey.Encrypt(plainData, false);
    }

    /// <summary>
    /// Descriptografa uma informação utilizando uma chave privada. 
    /// </summary>
    /// <param name="privateKey">A chave privada originária de um arquivo 
    /// XML.</param>
    /// <param name="data">O desafio do serviço de controle de acesso.</param>
    /// <returns>A informação descriptografado.</returns>
    public static byte[] Decrypt(RSACryptoServiceProvider privateKey,
                                  byte[] data) {
      return privateKey.Decrypt(data, false);
    }

    #endregion
  }
}