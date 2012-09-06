namespace tecgraf.openbus {
  /// <summary>
  /// Callback de despacho de chamada.
  /// 
  /// Interface a ser implementada pelo objeto de callback a ser chamado quando
  /// uma chamada proveniente de um barramento é recebida, que define a conexão
  /// a ser utilizada na validação dessa chamada.
  /// </summary>
  public interface CallDispatchCallback {
    /// <summary>
    /// Callback de login inválido.
    /// 
    /// Método a ser implementado pelo objeto de callback a ser chamado quando
    /// uma chamada proveniente de um barramento é recebida. Esse método é chamado
    /// para determinar a conexão a ser utilizada na validação de cada chamada
    /// recebida. Se a conexão informada não estiver conectada ao mesmo barramento
    /// indicado pelo parâmetro 'busid', a chamada provavelmente será recusada com
    /// um CORBA::NO_PERMISSION{InvalidLogin} pelo fato do login provavelmente não
    /// ser válido no barramento da conexão. Como resultado disso o cliente da
    /// chamada poderá indicar que o servidor não está implementado corretamente e
    /// lançar a exceção CORBA::NO_PERMISSION{InvalidRemote}. Caso alguma exceção
    /// ocorra durante a execução do método e não seja tratada, o erro será
    /// capturado pelo interceptador e registrado no log.
    /// </summary>
    /// <param name="context">Gerenciador de contexto do ORB que recebeu a chamada.</param>
    /// <param name="busid">Identificação do barramento através do qual a chamada foi
    ///              feita.</param>
    /// <param name="loginId">Informações do login do cliente da chamada.</param>
    /// <param name="uri">Identificador opaco descrevendo o objeto sendo chamado.</param>
    /// <param name="operation">Nome da operação sendo chamada.</param>
    /// <returns>Conexão a ser utilizada para receber a chamada.</returns>
    Connection Dispatch(OpenBusContext context, string busid, string loginId,
                        string uri, string operation);
  };
}