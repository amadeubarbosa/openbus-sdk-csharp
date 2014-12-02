namespace tecgraf.openbus.assistant {
  /// <summary>
  /// Define que o assistente deve efetuar login no barramento utilizando
  /// autenticação por senha.
  /// </summary>
  public class PasswordProperties : AssistantPropertiesImpl {
    /// <summary>
    /// Define que o assistente deve efetuar login no barramento utilizando
    /// autenticação por senha.
    /// 
    /// Assistentes criados com essas propriedades realizam o login no 
    /// barramento sempre utilizando autenticação da entidade indicada pelo 
    /// parâmetro 'entity' e a senha fornecida pelo parâmetro 'password'.
    /// </summary>
    /// <param name="entity">Identificador da entidade a ser autenticada.</param>
    /// <param name="password">Senha de autenticação no barramento da entidade.</param>
    /// <param name="domain">Identificador do domínio de autenticação.</param>
    public PasswordProperties(string entity, byte[] password, string domain) {
      Entity = entity;
      Password = password;
      Domain = domain;
      Type = LoginType.Password;
    }

    /// <summary>
    /// Senha de autenticação no barramento da entidade.
    /// </summary>
    public byte[] Password { get; private set; }

    /// <summary>
    /// Identificador da entidade a ser autenticada.
    /// </summary>
    public string Entity { get; private set; }

    /// <summary>
    /// Domínio de autenticação.
    /// </summary>
    public string Domain { get; private set; }
  }
}
