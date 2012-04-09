namespace tecgraf.openbus.sdk.multiplexed {
/**
 * Interface com operações para gerenciar acesso multiplexado a diferentes
 * barramentos OpenBus usando um mesmo ORB.
 * 
 * @author Tecgraf
 */

  public interface ConnectionMultiplexer {
    /// <summary>
    /// Define a conexão com o barramento a ser utilizada em todas as chamadas
    /// feitas pela thread corrente. Quando for <code>null</code> a thread passa 
    /// a ficar sem nenhuma conexão associada.
    /// </summary>
    Connection CurrentConnection { get; set; }

    /// <summary>
    /// Define a conexão a ser utilizada para receber chamadas oriundas de um dado
    /// barramento. Quando 'conn' for <code>null</code> o barramento passa a ficar sem
    /// nenhuma conexão associada. Sempre que o barramento não possuir uma
    /// conexão associada, todas as chamadas oriundas daquele barramento serão
    /// negadas com a exceção ({@link NO_PERMISSION}).
    /// </summary>
    /// <param name="busId"> Identificador do barramento ao qual a conexão será associada.</param>
    /// <param name="conn"> Conexão a barramento a ser associada a thread corrente.</param>
    void SetIncomingConnection(string busId, Connection conn);

    /// <summary>
    /// Devolve a conexão a ser utilizada para recerber chamadas oriundas de um
    /// dado barramento, ou <code>null</code> caso não haja nenhuma conexão
    /// associada ao barramento.
    /// </summary>
    /// <param name="busId"> Identificador do barramento ao qual a conexão está associada.</param>
    /// <returns> Conexão associada ao barramento.</returns>
    Connection GetIncomingConnection(string busId);
  }
}