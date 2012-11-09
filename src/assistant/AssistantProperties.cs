using omg.org.CORBA;

namespace tecgraf.openbus.assistant {
  /// <summary>
  /// Diferentes modos de autenticação de entidades.
  /// </summary>
  public enum LoginType {
    /// <summary>
    /// Representa a autenticação por nome de entidade e senha.
    /// </summary>
    Password,

    /// <summary>
    /// Representa a autenticação por nome de entidade e chave privada.
    /// </summary>
    PrivateKey,

    /// <summary>
    /// Representa a autenticação por autenticação compartilhada.
    /// </summary>
    SharedAuth
  }

  /// <summary>
  /// Representa um conjunto de parâmetros opcionais que podem ser utilizados 
  /// para definir parâmetros de configuração do Assistente.
  /// </summary>
  public interface AssistantProperties {
    /// <summary>
    /// Tempo em milisegundos indicando o tempo mínimo de espera antes de cada nova 
    /// tentativa após uma falha na execução de uma tarefa. Por exemplo, depois 
    /// de uma falha na tentativa de um login ou registro de oferta, o 
    /// assistente espera pelo menos o tempo indicado por esse parâmetro antes 
    /// de realizar uma nova tentativa.
    /// </summary>
    int Interval { get; set; }

    /// <summary>
    /// O ORB a ser utilizado pelo assistente para realizar suas tarefas. O 
    /// assistente também configura esse ORB de forma que todas as chamadas 
    /// feitas por ele sejam feitas com a identidade do login estabelecido pelo 
    /// assistente.
    /// 
    /// Na versão atual do IIOP.Net a implementação do ORB é um singleton e,
    /// portanto, há sempre apenas uma instância de ORB.
    /// </summary>
    OrbServices ORB { get; }

    /// <summary>
    /// Propriedades da conexão a ser criada com o barramento especificado. 
    /// Para maiores informações sobre essas propriedades, veja a operação
    /// 'OpenBusContext::CreateConnection()'.
    /// </summary>
    ConnectionProperties ConnectionProperties { get; set; }

    /// <summary>
    /// Callback que recebe notificações de falhas de logins realizados pelo 
    /// assistente.
    /// </summary>
    OnLoginFailure LoginFailureCallback { get; set; }

    /// <summary>
    /// Callback que recebe notificações de falhas de registros realizados pelo 
    /// assistente.
    /// </summary>
    OnRegisterFailure RegisterFailureCallback { get; set; }

    /// <summary>
    /// Callback que recebe notificações de falhas de remoções de ofertas 
    /// realizadas pelo assistente.
    /// </summary>
    OnRemoveOfferFailure RemoveOfferFailureCallback { get; set; }

    /// <summary>
    /// Callback que recebe notificações de falhas de buscas realizadas pelo 
    /// assistente.
    /// </summary>
    OnFindFailure FindFailureCallback { get; set; }

    /// <summary>
    /// Callback que recebe notificações de falhas de inicializações de 
    /// autenticação compartilhada realizadas pelo assistente.
    /// </summary>
    OnStartSharedAuthFailure StartSharedAuthFailureCallback { get; set; }

    /// <summary>
    /// Tipo de login que será usado pelo assistente.
    /// </summary>
    LoginType Type { get; }
  }
}