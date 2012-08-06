using tecgraf.openbus.core.v2_0.services.access_control;

namespace tecgraf.openbus {

  /// <summary>
  /// Callback de login inválido.
  /// 
  /// Interface a ser implementada pelo objeto de callback a ser chamado quando
  /// uma notificação de login inválido é recebida.
  /// </summary>
  public interface InvalidLoginCallback {

    /// <summary>
    /// Método que será chamado quando uma notificação de login inválido for
    /// recebida. Caso alguma exceção ocorra durante a execução do método e não
    /// seja tratada, o erro será capturado pelo interceptador e registrado no
    /// log.
    /// </summary>
    /// <param name="conn">Conexão que recebeu a notificação de login inválido.</param>
    /// <param name="login">Informações do login que se tornou inválido.</param>
    void InvalidLogin(Connection conn, LoginInfo login);
  }
}
