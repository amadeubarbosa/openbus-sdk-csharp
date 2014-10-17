using tecgraf.openbus.core.v2_1.services.access_control;

namespace tecgraf.openbus.assistant {
  /// <summary>
  /// Delegate a ser chamado para a obtenção dos dados necessários para uma 
  /// autenticação compartilhada.
  /// 
  /// Esses dados em geral têm uma vida útil potencialmente curta, portanto é 
  /// necessário que a aplicação proveja uma forma de obtê-los quando 
  /// necessário.
  /// </summary>
  /// <param name="secret">Segredo a ser fornecido na conclusão do processo de login.</param>
  /// <returns>Objeto que represeta o processo de login iniciado.</returns>
  public delegate LoginProcess SharedAuthHandler(out byte[] secret);

  /// <summary>
  /// Define que o assistente deve efetuar login no barramento utilizando
  /// autenticação compartilhada.
  /// </summary>
  public class SharedAuthProperties : AssistantPropertiesImpl {
    /// <summary>
    /// Define que o assistente deve efetuar login no barramento utilizando
    /// autenticação compartilhada.
    /// 
    /// Assistentes criados com essas propriedades realizam o login no 
    /// barramento sempre utilizando autenticação compartilhada. Os dados 
    /// necessários para tal são obtidos através da callback fornecida pelo 
    /// parâmetro 'callback'.
    /// </summary>
    /// <param name="callback">Delegate a ser chamado para a obtenção dos dados 
    /// necessários para uma autenticação compartilhada.</param>
    public SharedAuthProperties(SharedAuthHandler callback) {
      Callback = callback;
      Type = LoginType.SharedAuth;
    }

    /// <summary>
    /// Delegate a ser chamado para a obtenção dos dados necessários para uma autenticação compartilhada.
    /// </summary>
    public SharedAuthHandler Callback { get; private set; }
  }
}