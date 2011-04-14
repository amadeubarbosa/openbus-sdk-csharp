using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Xml;
using OpenbusAPI.Logger;

namespace OpenbusAPI.Security
{
  /// <summary>
  /// Classe respons�vel pela seguran�a do Openbus.
  /// </summary>
  public static class Crypto
  {
    #region Members

    /// <summary>
    /// L� uma chave privada a partir de um arquivo XML.
    /// </summary>
    /// <param name="privateKeyFileName">O arquivo XML.</param>
    /// <returns>Uma inst�ncia de RSACryptoServiceProvider.</returns>
    public static RSACryptoServiceProvider ReadPrivateKey(String privateKeyFileName) {
      StreamReader key = File.OpenText(privateKeyFileName);
      String pemstr = key.ReadToEnd().Trim();
      key.Close();

      RSACryptoServiceProvider rsa = RSAPKCS8KeyFormatter.DecodePEMKey(pemstr);
      if (rsa == null)
        Log.CRYPTO.Fatal("N�o foi poss�vel gerar um RSACryptoServiceProvider");

      return rsa;
    }

    /// <summary>
    /// L� um certificado digital a partir de um arquivo.
    /// </summary>
    /// <param name="certificateFile">O arquivo.</param>
    /// <returns>O certificado formatado em X509.</returns>
    /// <exception cref="System.Security.Cryptography.CryptographicException">
    /// Caso o arquivo n�o exista, esteja incorreto ou inv�lido.</exception>
    public static X509Certificate2 ReadCertificate(String certificateFile) {
      return new X509Certificate2(certificateFile);
    }

    /// <summary>
    /// Gera a resposta para o desafio enviado pelo servi�o de controle 
    /// de acesso.
    /// </summary>
    /// <param name="challenge">O desafio do servi�o de controle de acesso.
    /// </param>
    /// <param name="privateKey">A chave privada origin�ria de um arquivo 
    /// XML.</param>
    /// <param name="acsCertificate">O certificado formatado em X509.</param>
    /// <returns>A resposta do desafio.</returns>
    /// <exception cref="CryptographicException">As chaves n�o est�o corretas.
    /// </exception>
    public static byte[] GenerateAnswer(byte[] challenge,
      RSACryptoServiceProvider privateKey, X509Certificate2 acsCertificate) {
      byte[] plainChallenge = Decrypt(privateKey, challenge);
      return Encrypt(acsCertificate, plainChallenge);
    }

    #endregion

    #region Internal Members

    /// <summary>
    /// Descriptografa uma informa��o utilizando uma chave privada. 
    /// </summary>
    /// <param name="privateKey">A chave privada origin�ria de um arquivo 
    /// XML.</param>
    /// <param name="data">O desafio do servi�o de controle de acesso.</param>
    /// <returns>A informa��o descriptografado.</returns>
    private static byte[] Decrypt(RSACryptoServiceProvider privateKey, byte[] data) {
      return privateKey.Decrypt(data, false);
    }

    /// <summary>
    /// Encripta uma informa��o utilizando um certificado digital.
    /// </summary>
    /// <param name="acsCertificate">O certificado Digital.</param>
    /// <param name="plainData">A informa��o descriptografada.</param>
    /// <returns>A informa��o encriptografada.</returns>
    private static byte[] Encrypt(X509Certificate2 acsCertificate,
      byte[] plainData) {
      RSACryptoServiceProvider RsaProvider = acsCertificate.PublicKey.Key as
        RSACryptoServiceProvider;
      return RsaProvider.Encrypt(plainData, false);
    }

    #endregion
  }

}

