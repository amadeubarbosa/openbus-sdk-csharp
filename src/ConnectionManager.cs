using System;
using System.Collections.Generic;
using omg.org.CORBA;

namespace tecgraf.openbus {
/**
 * Interface com operações para gerenciar acesso multiplexado a diferentes
 * barramentos OpenBus usando um mesmo ORB.
 * 
 * @author Tecgraf
 */

  public interface ConnectionManager {
    /// <summary>
    /// ORB utilizado pela conexão. Como o IIOP.Net atualmente o ORB é um 
    /// singleton, a instância será sempre a mesma e pode ser obtida de outras
    /// formas.
    /// </summary>
    ORB ORB { get; }

    /// <summary>
    /// Cria uma conexão para um barramento a partir de um endereço de rede IP e
    /// uma porta.
    /// </summary>
    /// <param name="host">Endereço de rede IP onde o barramento está executando.</param>
    /// <param name="port">Porta do processo do barramento no endereço indicado.</param>
    /// <param name="props">Conjunto de propriedades que descrevem aspectos de uma conexão.</param>
    /// <returns>Conexão ao barramento referenciado.</returns>
    Connection CreateConnection(string host, ushort port,
                                IDictionary<string, string> props);

    /// <summary>
    /// Define a conexão a ser utilizada nas chamadas realizadas e no despacho de
    /// chamadas recebidas sempre que não houver uma conexão específica definida.
    /// Sempre que não houver uma conexão associada tanto as chamadas realizadas
    /// como as chamadas recebidas são negadas com a exceção CORBA::NO_PERMISSION.
    /// </summary>
    Connection DefaultConnection { get; set; }

    /// <summary>
    /// Define a conexão com o barramento a ser utilizada em todas as chamadas
    /// feitas pela thread corrente. Quando for <code>null</code> a thread passa 
    /// a ficar sem nenhuma conexão associada.
    /// </summary>
    Connection Requester { get; set; }

    /// <summary>
    /// Define a conexão a ser utilizada para receber chamadas oriundas do
    /// barramento ao qual está conectada, denominada conexão de despacho.
    /// </summary>
    /// <param name="conn"> Conexão a barramento a ser associada a thread corrente.</param>
    /// <exception cref="ArgumentNullException">A conexão é nula.</exception>
    void SetDispatcher(Connection conn);

    /// <summary>
    /// Devolve a conexão de despacho associada ao barramento indicado, se 
    /// houver dado barramento, ou <code>null</code> caso não haja nenhuma conexão
    /// associada ao barramento.
    /// </summary>
    /// <param name="busId"> Identificador do barramento ao qual a conexão está associada.</param>
    /// <returns> Conexão associada ao barramento.</returns>
    Connection GetDispatcher(string busId);

    /// <summary>
    /// Remove a conexão de despacho associada ao barramento indicado, se houver.
    /// </summary>
    /// <param name="busId"> Identificador do barramento ao qual a conexão está
    ///  associada.</param>
    /// <returns> Conexão a barramento associada ao barramento ou 'null' se não
    ///  houver nenhuma conexão associada.</returns>
    Connection ClearDispatcher(string busId);
  }
}