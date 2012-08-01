using System;
using System.Collections.Generic;
using omg.org.CORBA;
using tecgraf.openbus.exceptions;

namespace tecgraf.openbus {

  /// <summary>
  /// Gerencia conexões de acesso a barramentos OpenBus através de um ORB.
  ///
  /// Conexões representam formas diferentes de acesso ao barramento. O
  /// ConnectionManager permite criar essas conexões e gerenciá-las, indicando
  /// quais são utilizadas em cada chamada. As conexões são usadas basicamente
  /// de duas formas no tratamento das chamadas:
  /// - para realizar uma chamada remota (cliente), neste caso a conexão é
  ///   denominada "Requester".
  /// - para validar uma chamada recebida (servidor), neste caso a conexão é
  ///   denominada "Dispatcher".
  /// 
  /// Na versão atual do IIOP.Net, a implementação do ORB é um singleton e,
  /// portanto, há sempre apenas uma instância de ORB. Por isso, há sempre
  /// também apenas uma instância de ConnectionManager.
  /// </summary>
  public interface ConnectionManager {
    /// <summary>
    /// ORB associado ao ConnectionManager. Como no IIOP.Net atualmente o ORB
    /// é um singleton, a instância será sempre a mesma e pode ser obtida de
    /// outras formas.
    /// </summary>
    ORB ORB { get; }

    /// <summary>
	  /// Cria uma conexão para um barramento.
	  /// 
	  /// Cria uma conexão para um barramento. O barramento é indicado por um nome
	  /// ou endereço de rede e um número de porta, onde os serviços núcleo daquele
	  /// barramento estão executando.
    /// </summary>
    /// <param name="host"> Endereço ou nome de rede onde os serviços núcleo do
    /// barramento estão executando.</param>
    /// <param name="port"> Porta onde os serviços núcleo do barramento estão
    /// executando.</param>
    /// <param name="props">Lista opcional de propriedades que definem algumas
	  /// configurações sobre a forma que as chamadas realizadas ou validadas
	  /// com essa conexão são feitas. A seguir são listadas as propriedades
	  /// válidas:
	  /// - legacy.delegate: indica como é preenchido o campo 'delegate' das
	  ///   credenciais enviadas em chamadas usando protocolo OpenBus 1.5. Há
	  ///   duas formas possíveis (o padrão é 'caller'):
	  ///   - caller: o campo 'delegate' é preenchido sempre com a entidade
	  ///     do campo 'caller' da cadeia de chamadas.
	  ///   - originator: o campo 'delegate' é preenchido sempre com a
	  ///     entidade que originou a cadeia de chamadas, que é o primeiro
	  ///     login do campo 'originators' ou o campo 'caller' quando este
	  ///     é vazio.</param>
    /// <exception cref="InvalidBusAddressException">Os parâmetros 'host' e
    /// 'port' não são válidos.</exception>
    /// <exception cref="InvalidPropertyValueException">O valor de uma
    /// propriedade não é válido.</exception>
    /// <returns>Conexão criada.</returns>
    Connection CreateConnection(string host, ushort port,
                                IDictionary<string, string> props);

    /// <summary>
	  /// Define ou obtém a conexão padrão a ser usada nas chamadas.
	  /// 
	  /// Define ou obtém uma conexão a ser utilizada como "Requester" e
	  /// "Dispatcher" de chamadas sempre que não houver uma conexão "Requester"
	  /// e "Dispatcher" específica definida para o caso específico, como é feito
	  /// através das operações 'Requester' e 'SetDispatcher'.
    /// </summary>
    Connection DefaultConnection { get; set; }

    /// <summary>
	  /// Define ou obtém a conexão "Requester" do contexto corrente.
	  /// 
	  /// Define a conexão "Requester" a ser utilizada em todas as chamadas
	  /// feitas no contexto atual, por exemplo, o contexto representado pelo
	  /// 'CORBA::PICurrent' atual. Quando o valor recebido é 'null' o contexto
	  /// passa a ficar sem nenhuma conexão associada.
    /// </summary>
    Connection Requester { get; set; }

    /// <summary>
	  /// Define uma a conexão como "Dispatcher" de barramento.
	  /// 
	  /// Define a conexão como "Dispatcher" do barramento ao qual ela está
	  /// conectada, de forma que todas as chamadas originadas por entidades
	  /// conectadas a este barramento serão validadas com essa conexão. Só pode
	  /// haver uma conexão "Dispatcher" para cada barramento, portanto se já houver
	  /// outra conexão "Dispatcher" para o mesmo barramento essa será
	  /// substituída pela nova conexão.
    /// </summary>
    /// <param name="conn"> Conexão a ser definida como "Dispatcher".</param>
    /// <exception cref="ArgumentNullException">A conexão é nula.</exception>
    void SetDispatcher(Connection conn);

    /// <summary>
	  /// Devolve a conexão "Dispatcher" do barramento indicado.
    /// </summary>
    /// <param name="busId"> Identificador do barramento ao qual a conexão está
    /// associada.</param>
    /// <returns> Conexão "Dispatcher" do barramento indicado, ou 'null' caso não
    /// haja nenhuma conexão "Dispatcher" associada ao barramento indicado.</returns>
    Connection GetDispatcher(string busId);

    /// <summary>
	  /// Remove a conexão "Dispatcher" associada ao barramento indicado.
    /// </summary>
    /// <param name="busId"> Identificador do barramento ao qual a conexão está
    /// associada.</param>
    /// <returns> Conexão "Dispatcher" associada ao barramento ou 'null' se não
    /// houver nenhuma conexão associada.</returns>
    Connection ClearDispatcher(string busId);
  }
}