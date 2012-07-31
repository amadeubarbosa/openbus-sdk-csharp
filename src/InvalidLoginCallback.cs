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
    /// recebida.
    /// </summary>
    /// <param name="conn">Conexão que recebeu a notificação de login inválido.</param>
    /// <param name="login">Informações do login que se tornou inválido.</param>
    void InvalidLogin(Connection conn, LoginInfo login);
  }
}
