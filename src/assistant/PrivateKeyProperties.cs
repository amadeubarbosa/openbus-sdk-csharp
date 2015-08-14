using Org.BouncyCastle.Crypto;

namespace tecgraf.openbus.assistant {
  /// <summary>
  /// Define que o assistente deve efetuar login no barramento utilizando
  /// autenticação por certificado.
  /// </summary>
  public class PrivateKeyProperties : AssistantPropertiesImpl {
    /// <summary>
    /// Define que o assistente deve efetuar login no barramento utilizando
    /// autenticação por certificado.
    /// 
    /// Assistentes criados com essas propriedades realizam o login no 
    /// barramento sempre utilizando autenticação da entidade indicada pelo 
    /// parâmetro 'entity' e a chave privada fornecida pelo parâmetro 
    /// 'password'.
    /// </summary>
    /// <param name="entity">Identificador da entidade a ser autenticada.</param>
    /// <param name="privateKey">Chave privada correspondente ao certificado 
    /// registrado a ser utilizada na autenticação.</param>
    public PrivateKeyProperties(string entity, AsymmetricCipherKeyPair privateKey) {
      Entity = entity;
      PrivateKey = privateKey;
      Type = LoginType.PrivateKey;
    }

    /// <summary>
    /// Chave privada correspondente ao certificado registrado a ser utilizada na autenticação.
    /// </summary>
    public AsymmetricCipherKeyPair PrivateKey { get; private set; }

    /// <summary>
    /// Identificador da entidade a ser autenticada.
    /// </summary>
    public string Entity { get; private set; }
  }
}