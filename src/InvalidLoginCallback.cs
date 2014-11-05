using tecgraf.openbus.core.v2_1.services.access_control;

namespace tecgraf.openbus {
  /// <summary>
  /// Callback de login inválido.
  /// 
  /// Método que será chamado quando uma notificação de login inválido for
  /// recebida. Caso alguma exceção ocorra durante a execução do método e não
  /// seja tratada, o erro será capturado pelo interceptador e registrado no
  /// log.
  /// 
  /// É importante observar que a primeira chamada remota que esta callback
  /// realizar deve ser uma tentativa de autenticação junto ao barramento
  /// (loginBy*). Caso a callback realize qualquer outra chamada remota a mesma
  /// potencialmenete ocasionará um laço infinito, pois esta outra chamada irá
  /// falhar devido ao login invalidado, chamando novamente a callback para
  /// tentar refazer a autenticação.
  /// </summary>
  /// <param name="conn">Conexão que recebeu a notificação de login inválido.</param>
  /// <param name="login">Informações do login que se tornou inválido.</param>
  public delegate void InvalidLoginCallback(Connection conn, LoginInfo login);
}