using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Xml;

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
    /// <returns>A String que representa a chave.</returns>
    public static String ReadPrivateKey(String privateKeyFileName) {
      XmlDocument doc = new XmlDocument();
      doc.Load(privateKeyFileName);

      if (doc == null)
        return null;

      StringWriter writer = new StringWriter();
      doc.Save(writer);

      return writer.ToString();
    }

    /// <summary>
    /// L� um certificado digital a partir de um arquivo.
    /// </summary>
    /// <param name="certificateFile">O arquivo.</param>
    /// <returns>O certificado formatado em X509.</returns>
    public static X509Certificate2 ReadCertificate(String certificateFile) {
      return new X509Certificate2(certificateFile);
    }

    /// <summary>
    /// Gera a resposta para o desafio enviado pelo servi�o de controle de acesso.
    /// </summary>
    /// <param name="challenge">O desafio do servi�o de controle de acesso.</param>
    /// <param name="XmlPrivateKey">A chave privada origin�ria de um arquivo XML.</param>
    /// <param name="acsCertificate">O certificado formatado em X509.</param>
    /// <returns>A resposta do desafio.</returns>
    public static byte[] GenerateAnswer(byte[] challenge, String XmlPrivateKey,
      X509Certificate2 acsCertificate) {
      byte[] plainChallenge = Decrypt(XmlPrivateKey, challenge);
      return Encrypt(acsCertificate, plainChallenge);
    }

    #endregion

    #region Internal Members

    /// <summary>
    /// Descriptografa uma informa��o utilizando uma chave privada. 
    /// </summary>
    /// <param name="XmlPrivateKey">A chave privada origin�ria de um arquivo XML.</param>
    /// <param name="data">O desafio do servi�o de controle de acesso.</param>
    /// <returns>A informa��o descriptografado.</returns>
    private static byte[] Decrypt(String XmlPrivateKey, byte[] data) {
      RSACryptoServiceProvider rsaProvider = new RSACryptoServiceProvider();
      rsaProvider.FromXmlString(XmlPrivateKey);

      return rsaProvider.Decrypt(data, false);
    }

    /// <summary>
    /// Encripta uma informa��o utilizando um certificado digital.
    /// </summary>
    /// <param name="acsCertificate">O certificado Digital.</param>
    /// <param name="plainData">A informa��o descriptografada.</param>
    /// <returns>A informa��o encriptografada.</returns>
    private static byte[] Encrypt(X509Certificate2 acsCertificate, byte[] plainData) {
      RSACryptoServiceProvider RsaProvider = acsCertificate.PublicKey.Key as RSACryptoServiceProvider;
      return RsaProvider.Encrypt(plainData, false);
    }

    #endregion
  }

}

