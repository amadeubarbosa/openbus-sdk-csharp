﻿using System;
using omg.org.CORBA;
using tecgraf.openbus.core.v2_0.services.access_control;
using tecgraf.openbus.core.v2_0.services.offer_registry;

namespace tecgraf.openbus {
  /// <summary>
  /// Permite controlar o contexto das chamadas de um ORB para acessar
  /// informações que identificam essas chamadas em barramentos OpenBus.
  ///
  /// O contexto de uma chamada pode ser definido pela linha de execução atual
  /// do programa em que executa uma chamada, o que pode ser a thread em execução
  /// ou mais comumente o 'CORBA::PICurrent' do padrão CORBA. As informações
  /// acessíveis através do 'OpenBusContext' se referem basicamente à
  /// identificação da origem das chamadas, ou seja, nome das entidades que
  /// autenticaram os acessos ao barramento que originaram as chamadas.
  /// 
  /// A identifcação de chamadas no barramento é controlada pelo OpenBusContext 
  /// através da manipulação de duas abstrações representadas pelas seguintes
  /// interfaces:
  /// - Connection: Representa um acesso ao barramento, que é usado tanto para
  ///   fazer chamadas como para receber chamadas através do barramento. Para
  ///   tanto a conexão precisa estar autenticada, ou seja, logada. Cada chamada
  ///   feita através do ORB é enviada com as informações do login da conexão
  ///   associada ao contexto em que a chamada foi realizada. Cada chamada
  ///   recebida também deve vir através de uma conexão logada, que deve ser o
  ///   mesmo login com que chamadas aninhadas a essa chamada original devem ser
  ///   feitas.
  /// - CallChain: Representa a identicação de todos os acessos ao barramento que
  ///   originaram uma chamada recebida. Sempre que uma chamada é recebida e
  ///   executada, é possível obter um CallChain através do qual é possível
  ///   inspecionar as informações de acesso que originaram a chamada recebida.
  /// 
  /// Na versão atual do IIOP.Net a implementação do ORB é um singleton e,
  /// portanto, há sempre apenas uma instância de ORB. Por isso, há sempre
  /// também apenas uma instância de OpenBusContext.
  /// </summary>
  public interface OpenBusContext {
    /// <summary>
    /// ORB controlado por esse OpenBusContext. Como no IIOP.Net atualmente o ORB
    /// é um singleton, a instância será sempre a mesma e pode ser obtida de
    /// outras formas.
    /// </summary>
    OrbServices ORB { get; }

    /// <summary>
    /// Callback a ser chamada para determinar a conexão a ser utilizada
    /// para receber cada chamada.
    ///
    /// Esse atributo é utilizado para definir um objeto que implementa uma
    /// interface de callback a ser chamada sempre que a conexão receber uma do
    /// barramento. Essa callback deve devolver a conexão a ser utilizada para
    /// receber a chamada. A conexão utilizada para receber a chamada será
    /// a única conexão através da qual novas chamadas aninhadas à chamada
    /// recebida poderão ser feitas (veja a operação 'joinChain').
    ///
    /// Se o objeto de callback for definido como 'null' ou devolver 'null', a
    /// conexão padrão é utilizada para receber a chamada, caso esta esteja
    /// definida.
    ///
    /// Caso esse atributo seja 'null', nenhum objeto de callback é chamado na
    /// ocorrência desse evento e 
    /// </summary>
    CallDispatchCallback OnCallDispatch { get; set; }

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
    /// - access.key: chave de acesso a ser utiliza internamente para a
    ///   geração de credenciais que identificam as chamadas através do
    ///   barramento. A chave deve ser uma chave privada RSA de 2048 bits
    ///   (256 bytes). Quando essa propriedade não é fornecida, uma chave
    ///   de acesso é gerada automaticamente.
    /// - legacy.disable: desabilita o suporte a chamadas usando protocolo
    ///   OpenBus 1.5. Por padrão o suporte está habilitado.
    /// - legacy.delegate: indica como é preenchido o campo 'delegate' das
    ///   credenciais enviadas em chamadas usando protocolo OpenBus 1.5. Há
    ///   duas formas possíveis (o padrão é 'caller'):
    ///   - caller: o campo 'delegate' é preenchido sempre com a entidade
    ///     do campo 'caller' da cadeia de chamadas.
    ///   - originator: o campo 'delegate' é preenchido sempre com a
    ///     entidade que originou a cadeia de chamadas, que é o primeiro
    ///     login do campo 'originators' ou o campo 'caller' quando este
    ///     é vazio.</param>
    /// <exception cref="ArgumentException">Caso o host seja null ou vazio, ou a porta seja 0.</exception>
    /// <returns>Conexão criada.</returns>
    Connection CreateConnection(string host, ushort port,
                                ConnectionProperties props);

    /// <summary>
    /// Define uma conexão a ser utilizada em chamadas sempre que não houver uma
    /// conexão específica definida no contexto atual, como é feito através da
    /// operação 'CurrentConnection'. Quando o valor for 'null' nenhuma conexão
    /// fica definida como a conexão padrão.
    /// </summary>
    /// <param name="conn">Conexão a ser definida como conexão padrão.</param>
    /// <returns>Conexão definida como conexão padrão anteriormente, ou null se
    /// não havia conexão padrão definida ateriormente.</returns>
    Connection SetDefaultConnection(Connection conn);

    /// <summary>
    /// Devolve a conexão padrão.
    /// 
    /// Veja operação 'SetDefaultConnection'.
    /// </summary>
    /// <returns>Conexão definida como conexão padrão.</returns>
    Connection GetDefaultConnection();

    /// <summary>
    /// Define a conexão associada ao contexto corrente.
    /// 
    /// Define a conexão a ser utilizada em todas as chamadas feitas no contexto
    /// atual. Quando 'conn' é 'null' o contexto passa a ficar sem nenhuma conexão
    /// associada.
    /// </summary>
    /// <param name="conn">Conexão a ser associada ao contexto corrente.</param>
    /// <returns>Conexão definida como a conexão corrente anteriormente, ou null
    /// se não havia conexão definida ateriormente.</returns>
    Connection SetCurrentConnection(Connection conn);

    /// <summary>
    /// Devolve a conexão associada ao contexto corrente.
    /// 
    /// Devolve a conexão associada ao contexto corrente, que pode ter sido
    /// definida usando a operação 'SetCurrentConnection' ou
    /// 'SetDefaultConnection'.
    /// </summary>
    /// <returns>Conexão associada ao contexto corrente, ou 'null'
    /// caso não haja nenhuma conexão associada.</returns>
    Connection GetCurrentConnection();

    /// <summary>
    /// Devolve a cadeia de chamadas à qual a execução corrente pertence.
    /// 
    /// Caso a contexto corrente (e.g. definido pelo 'CORBA::PICurrent') seja o
    /// contexto de execução de uma chamada remota oriunda do barramento essa
    /// operação devolve um objeto que representa a cadeia de chamadas que esta
    /// chamada faz parte. Caso contrário, devolve 'null'.
    /// </summary>
    /// <returns>Cadeia da chamada em execução.</returns>
    CallerChain CallerChain { get; }

    /// <summary>
    /// Associa uma cadeia de chamadas ao contexto corrente.
    /// 
    /// Associa uma cadeia de chamadas ao contexto corrente (e.g. definido pelo
    /// 'CORBA::PICurrent'), de forma que todas as chamadas remotas seguintes
    /// neste mesmo contexto sejam feitas como parte dessa cadeia de chamadas.
    /// </summary>
    /// <param name="chain">Cadeia de chamadas a ser associada ao contexto corrente.</param>
    void JoinChain(CallerChain chain);

    /// <summary>
    /// Faz com que nenhuma cadeia de chamadas esteja associada ao contexto
    /// corrente.
    /// 
    /// Remove a associação da cadeia de chamadas ao contexto corrente (e.g.
    /// definido pelo 'CORBA::PICurrent'), fazendo com que todas as chamadas
    /// seguintes feitas neste mesmo contexto deixem de fazer parte da cadeia de
    /// chamadas associada previamente. Ou seja, todas as chamadas passam a
    /// iniciar novas cadeias de chamada.
    /// </summary>
    void ExitChain();

    /// <summary>
    /// Devolve a cadeia de chamadas associada ao contexto corrente.
    /// 
    /// Devolve um objeto que representa a cadeia de chamadas associada ao
    /// contexto corrente (e.g. definido pelo 'CORBA::PICurrent'). A cadeia de
    /// chamadas informada foi associada previamente pela operação 'JoinChain'.
    /// Caso o contexto corrente não tenha nenhuma cadeia associada, essa operação
    /// devolve 'null'.
    /// </summary>
    /// <returns>Cadeia de chamadas associada ao contexto corrente ou 'null'.</returns>
    CallerChain JoinedChain { get; }

    /// <summary>
    /// Referência ao serviço núcleo de registro de logins do barramento
    /// referenciado no contexto atual.
    /// </summary>
    LoginRegistry LoginRegistry { get; }

    /// <summary>
    /// Referência ao serviço núcleo de registro de ofertas do barramento
    /// referenciado no contexto atual.
    /// </summary>
    OfferRegistry OfferRegistry { get; }
  }
}