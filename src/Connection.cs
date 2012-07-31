using System;
using omg.org.CORBA;
using tecgraf.openbus.core.v2_0.services;
using tecgraf.openbus.core.v2_0.services.access_control;
using tecgraf.openbus.core.v2_0.services.offer_registry;
using tecgraf.openbus.exceptions;

namespace tecgraf.openbus {
  /// <summary>
  /// Objeto que representa uma forma de acesso a um barramento.
  ///
  /// Uma conexão representa uma forma de acesso a um barramento. Basicamente, uma
  /// conexão é usada para representar uma identidade de acesso a um barramento.
  /// É possível uma aplicação assumir múltiplas identidades ao acessar um ou mais
  /// barramentos criando múltiplas conexões para esses barramentos.
  /// 
  /// Para que as conexões possam ser efetivamente utilizadas elas precisam estar
  /// logadas ao barramento, que pode ser visto como um identificador de acesso.
  /// Cada login possui um identificador único e é autenticado em nome de uma
  /// entidade, que pode representar um sistema computacional ou mesmo uma pessoa.
  /// A função da entidade é atribuir responsabilidade às chamadas feitas com
  /// aquele login.
  /// 
  /// É importante notar que a conexão só define uma forma de acesso, mas não é
  /// usada diretamente pela aplicação ao realizar ou receber chamadas, pois as
  /// chamadas ocorrem usando proxies e servants de um ORB. As conexões que são
  /// efetivamente usadas nas chamadas do ORB são definidas através do
  /// ConnectionManager associado a este ORB.
  /// 
  /// Na versão atual do IIOP.Net, a implementação do ORB é um singleton e,
  /// portanto, há sempre apenas uma instância de ORB. Por isso, há sempre
  /// também apenas uma instância de ConnectionManager.
  /// </summary>
  public interface Connection {

    /// <summary>
    /// ORB correspondente ao ConnectionManager a partir do qual essa conexão
    /// foi criada. 
    /// </summary>
    ORB ORB { get; }

    /// <summary>
	  /// Referência ao serviço núcleo de registro de ofertas do barramento ao qual
	  /// a conexão se refere.
    /// </summary>
    OfferRegistry Offers { get; }

    /// <summary>
	  /// Identificador do barramento ao qual essa conexão se refere.
    /// </summary>
    string BusId { get; }

    /// <summary>
	  /// Informações do login dessa conexão ou 'null' se a conexão não está logada,
	  /// ou seja, não tem um login válido no barramento.
    /// </summary>
    LoginInfo? Login { get; }

    /// <summary>
	  /// A autenticação por senha é validada usando um dos validadores de senha
	  /// definidos pelo adminsitrador do barramento.
    /// </summary>
    /// <param name="entity">Identificador da entidade a ser autenticada.</param>
    /// <param name="password">Senha de autenticação da entidade no barramento.</param>
    /// <exception cref="AccessDenied"> A senha fornecida para autenticação da 
    /// entidade não foi validada pelo barramento.</exception>
    /// <exception cref="AlreadyLoggedInException">A conexão já está logada.</exception>
    /// <exception cref="BusChangedException"> O identificador do barramento mudou. Uma nova conexão
    /// deve ser criada.</exception>
    /// <exception cref="ServiceFailure">Ocorreu uma falha interna nos serviços do
    /// barramento que impediu a autenticação da conexão.</exception>
    void LoginByPassword(String entity, Byte[] password);
 
    /// <summary>
    /// Efetua login de uma entidade usando autenticação por certificado.
    /// 
	  /// A autenticação por certificado é validada usando um certificado de login
	  /// registrado pelo adminsitrador do barramento.
    /// </summary>
    /// <param name="entity"> Identificador da entidade a ser autenticada.</param>
    /// <param name="privKey"> Chave privada correspondente ao certificado registrado
	  /// a ser utilizada na autenticação.</param>
    /// <exception cref="AccessDenied"> A chave privada fornecida não corresponde ao
    /// certificado da entidade registrado no barramento indicado.</exception>
    /// <exception cref="AlreadyLoggedInException"> A conexão já está logada.</exception>
    /// <exception cref="BusChangedException"> O identificador do barramento mudou. Uma nova conexão
    /// deve ser criada.</exception>
    /// <exception cref="InvalidPrivateKeyException"> A chave privada fornecida não é válida.</exception>
    /// <exception cref="MissingCertificate"> Não há certificado para essa entidade
    /// registrado no barramento indicado.</exception>
    /// <exception cref="ServiceFailure"> Ocorreu uma falha interna nos serviços
    /// do barramento que impediu a autenticação da conexão.</exception>
    void LoginByCertificate(String entity, Byte[] privKey);

    /// <summary>
    /// \brief Inicia o processo de login por autenticação compartilhada.
    /// 
    /// A autenticação compartilhada permite criar um novo login compartilhando a
    /// mesma autenticação do login atual da conexão. Portanto essa operação só
    /// pode ser chamada enquanto a conexão estiver logada, caso contrário a
    /// exceção de sistema CORBA::NO_PERMISSION{NoLogin} é lançada. As informações
    /// fornecidas por essa operação devem ser passadas para a operação
    /// 'loginBySharedAuth' para conclusão do processo de login por autenticação
    /// compartilhada. Isso deve ser feito dentro do tempo de lease definido pelo
    /// administrador do barramento. Caso contrário essas informações se tornam
    /// inválidas e não podem mais ser utilizadas para criar um login.
    /// </summary>
    /// <param name="secret"> Segredo a ser fornecido na conclusão do processo de login.</param>
    /// <returns> Objeto que represeta o processo de login iniciado.</returns>
    /// <exception cref="ServiceFailure"> Ocorreu uma falha interna nos serviços
    /// do barramento que impediu a obtenção do objeto de login e segredo.</exception>
    LoginProcess StartSharedAuth(out Byte[] secret);

    /// <summary>
	  /// Efetua login de uma entidade usando autenticação compartilhada.
	  /// 
	  /// A autenticação compartilhada é feita a partir de informações obtidas a
	  /// através da operação 'StartSharedAuth' de uma conexão logada.
    /// </summary>
    /// <param name="login"> Objeto que represeta o processo de login iniciado.</param>
    /// <param name="secret"> Segredo a ser fornecido na conclusão do processo de login.</param>
    /// <exception cref="AccessDenied"> O segredo fornecido não corresponde ao esperado
    /// pelo barramento.</exception>
    /// <exception cref="AlreadyLoggedInException"> A conexão já está logada.</exception>
    /// <exception cref="BusChangedException"> O identificador do barramento mudou. Uma nova conexão
    /// deve ser criada.</exception>
    /// <exception cref="InvalidLoginProcessException"> O LoginProcess informado é inválido, por
    /// exemplo depois de ser cancelado ou ter expirado.</exception>
    /// <exception cref="ServiceFailure"> Ocorreu uma falha interna nos serviços
    /// do barramento que impediu a autenticação da conexão.</exception>
    void LoginBySharedAuth(LoginProcess login, Byte[] secret);

    /// <summary>
	  /// Efetua logout da conexão, tornando o login atual inválido.
	  /// 
	  /// Após a chamada a essa operação a conexão fica deslogada, implicando que
	  /// qualquer chamada realizada pelo ORB usando essa conexão resultará numa
	  /// exceção de sistema 'CORBA::NO_PERMISSION{NoLogin}' e chamadas recebidas
	  /// por esse ORB serão respondidas com a exceção
	  /// 'CORBA::NO_PERMISSION{UnknownBus}' indicando que não foi possível
	  /// validar a chamada pois a conexão está temporariamente deslogada.
    /// </summary>
    /// <returns>Verdadeiro se o processo de logout for concluído com êxito e 
    /// falso se a conexão já estiver deslogada (login inválido).</returns>
    bool Logout();

    /// <summary>
	  /// Callback a ser chamada quando o login atual se tornar inválido.
	  ///
	  /// Esse atributo é utilizado para definir um objeto que implementa uma
	  /// interface de callback a ser chamada sempre que a conexão receber uma
	  /// notificação de que o seu login está inválido. Essas notificações ocorrem
	  /// durante chamadas realizadas ou recebidas pelo barramento usando essa
	  /// conexão. Um login pode se tornar inválido caso o administrador
	  /// explicitamente o torne inválido ou caso a thread interna de renovação de
	  /// login não seja capaz de renovar o lease do login a tempo. Caso esse
	  /// atributo seja 'null', nenhum objeto de callback é chamado na ocorrência
	  /// desse evento.
	  ///
	  /// Durante a execução dessa callback um novo login pode ser restabelecido.
	  /// Neste caso, a chamada do barramento que recebeu a notificação de login
	  /// inválido é refeita usando o novo login, caso contrário, a chamada original
	  /// lança a exceção de de sistema 'CORBA::NO_PERMISSION{NoLogin}'.
    /// </summary>
    InvalidLoginCallback OnInvalidLogin { get; set; }
     
    /// <summary>
	  /// Devolve a cadeia de chamadas à qual a execução corrente pertence.
	  /// 
	  /// Caso a contexto corrente (e.g. definido pelo 'CORBA::PICurrent') seja o
	  /// contexto de execução de uma chamada remota oriunda do barramento dessa
	  /// conexão, essa operação devolve um objeto que representa a cadeia de
	  /// chamadas do barramento que esta chamada faz parte. Caso contrário, 
	  /// devolve 'null'.
    /// </summary>
    /// <returns> Cadeia da chamada em execução.</returns>
    CallerChain CallerChain { get; }
     
    /// <summary>
	  /// Associa uma cadeia de chamadas ao contexto corrente.
	  /// 
	  /// Associa uma cadeia de chamadas ao contexto corrente (e.g. definido pelo
	  /// 'CORBA::PICurrent'), de forma que todas as chamadas remotas seguintes
	  /// neste mesmo contexto sejam feitas como parte dessa cadeia de chamadas.
    /// </summary>
    /// <param name="chain"> Cadeia de chamadas a ser associada ao contexto corrente.</param>
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
	  /// contexto corrente (e.g. definido pelo 'CORBA::PICurrent') nesta conexão.
	  /// A cadeia de chamadas informada foi associada previamente pela operação
	  /// 'joinChain'. Caso o contexto corrente não tenha nenhuma cadeia associada,
	  /// essa operação devolve 'null'.
    /// </summary>
    /// <returns> Cadeia de chamadas associada ao contexto corrente ou 'null'.</returns>
    CallerChain JoinedChain { get; }
   }
}
