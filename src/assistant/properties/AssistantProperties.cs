using omg.org.CORBA;

namespace tecgraf.openbus.assistant.properties {
  /// <summary>
  /// Identifica o tipo de login que será usado pelo assistente.
  /// </summary>
  public enum LoginType {
    /// <summary>
    /// O login será feito por nome de entidade e senha.
    /// </summary>
    Password,
    /// <summary>
    /// O login será feito por nome de entidade e chave privada.
    /// </summary>
    PrivateKey,
    /// <summary>
    /// O login será feito por autenticação compartilhada.
    /// </summary>
    SharedAuth
  }

  /// <summary>
  /// 
  /// </summary>
  public interface AssistantProperties {
    /// <summary>
    /// 
    /// </summary>
    int Interval { get; set; }

    /// <summary>
    /// 
    /// </summary>
    ORB ORB { get; }

    /// <summary>
    /// 
    /// </summary>
    ConnectionProperties ConnectionProperties { get; set; }

    /// <summary>
    /// 
    /// </summary>
    OnFailureCallback FailureCallback { get; set; }

    /// <summary>
    /// Tipo de login que será usado pelo assistente.
    /// </summary>
    LoginType Type { get; }
  }
}
