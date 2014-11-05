using System;
using omg.org.CORBA;
using tecgraf.openbus.core.v2_1.services;
using tecgraf.openbus.core.v2_1.services.access_control;
using tecgraf.openbus.exceptions;

namespace tecgraf.openbus {
  /// <summary>
  /// Conexão para acesso identificado a um barramento.
  ///
  /// Uma conexão é usada para realizar acessos identificados a um barramento.
  /// Denominamos esses acessos identificados ao barramento de login. Cada login
  /// possui um identificador único e está sempre associado ao nome de uma
  /// entidade que é autenticada no momento do estabelecimento do login.
  /// Há basicamente três formas de autenticação de entidade disponíveis:
  /// - Por Senha: veja a operação 'LoginByPassword'
  /// - Por Certificado de login: veja a operação 'LoginByCertificate'
  /// - Por Autenticação compartilhada: veja a operação 'LoginBySharedAuth'
  ///
  /// A entidade associada ao login é responsável por todas as chamadas feitas
  /// através daquela conexão e essa entidade deve ser levada em consideração
  /// pelos serviços ofertados no barramento para decidir aceitar ou recusar
  /// chamadas.
  ///
  /// É possível uma aplicação assumir múltiplas identidades ao acessar um ou mais
  /// barramentos criando múltiplas conexões para esses barramentos.
  /// 
  /// É importante notar que a conexão não é usada diretamente pela aplicação ao
  /// realizar ou receber chamadas, pois as chamadas ocorrem usando proxies e
  /// servants de um ORB. As conexões que são efetivamente usadas nas chamadas do
  /// ORB são definidas através do OpenBusContext associado ao ORB.
  /// 
  /// Na versão atual do IIOP.Net a implementação do ORB é um singleton e,
  /// portanto, há sempre apenas uma instância de ORB. Por isso, há sempre
  /// também apenas uma instância de OpenBusContext.
  /// </summary>
  public interface Connection {
    /// <summary>
    /// ORB correspondente ao OpenBusContext a partir do qual essa conexão
    /// foi criada. 
    /// </summary>
    OrbServices ORB { get; }

    /// <summary>
    /// Identificador do barramento ao qual essa conexão se refere.
    /// </summary>
    string BusId { get; }

    /// <summary>
    /// Informações do login dessa conexão ou 'null' se a conexão não está
    /// autenticada, ou seja, não tem um login válido no barramento.
    /// </summary>
    LoginInfo? Login { get; }

    /// <summary>
    /// Efetua login de uma entidade usando autenticação por senha.
    /// 
    /// A autenticação por senha é validada usando um dos validadores de senha
    /// definidos pelo adminsitrador do barramento.
    /// </summary>
    /// <param name="entity">Identificador da entidade a ser autenticada.</param>
    /// <param name="password">Senha de autenticação da entidade no barramento.</param>
    /// <exception cref="ArgumentException">Caso a entidade ou a senha sejam nulas.</exception>
    /// <exception cref="AccessDenied"> A senha fornecida para autenticação da 
    /// entidade não foi validada pelo barramento.</exception>
    /// <exception cref="AlreadyLoggedInException">A conexão já está autenticada.</exception>
    /// <exception cref="ServiceFailure">Ocorreu uma falha interna nos serviços do
    /// barramento que impediu a autenticação da conexão.</exception>
    void LoginByPassword(String entity, Byte[] password);

    /// <summary>
    /// Efetua login de uma entidade usando autenticação por certificado.
    /// 
    /// A autenticação por certificado é validada usando um certificado de login
    /// registrado pelo administrador do barramento.
    /// </summary>
    /// <param name="entity"> Identificador da entidade a ser autenticada.</param>
    /// <param name="privateKey"> Chave privada correspondente ao certificado registrado
    /// a ser utilizada na autenticação, no formato esperado pelo OpenBus.</param>
    /// <exception cref="ArgumentException">Caso a entidade seja nula ou a chave privada seja nula ou não tenha sido gerada pelo SDK do OpenBus.</exception>
    /// <exception cref="AccessDenied"> A chave privada fornecida não corresponde ao
    /// certificado da entidade registrado no barramento indicado.</exception>
    /// <exception cref="AlreadyLoggedInException"> A conexão já está autenticada.</exception>
    /// <exception cref="MissingCertificate"> Não há certificado para essa entidade
    /// registrado no barramento indicado.</exception>
    /// <exception cref="ServiceFailure"> Ocorreu uma falha interna nos serviços
    /// do barramento que impediu a autenticação da conexão.</exception>
    void LoginByCertificate(String entity, PrivateKey privateKey);

    /// <summary>
    /// Inicia o processo de login por autenticação compartilhada.
    /// 
    /// A autenticação compartilhada permite criar um novo login compartilhando a
    /// mesma autenticação do login atual da conexão. Portanto essa operação só
    /// pode ser chamada enquanto a conexão estiver autenticada, caso contrário a
    /// exceção de sistema CORBA::NO_PERMISSION{NoLogin} é lançada. As informações
    /// fornecidas por essa operação devem ser passadas para a operação
    /// 'loginBySharedAuth' para conclusão do processo de login por autenticação
    /// compartilhada. Isso deve ser feito dentro do tempo de lease definido pelo
    /// administrador do barramento. Caso contrário essas informações se tornam
    /// inválidas e não podem mais ser utilizadas para criar um login.
    /// </summary>
    /// <param name="secret"> Segredo a ser fornecido na conclusão do processo de login.</param>
    /// <returns> Objeto que representa o processo de login iniciado.</returns>
    /// <exception cref="ServiceFailure"> Ocorreu uma falha interna nos serviços
    /// do barramento que impediu a obtenção do objeto de login e segredo.</exception>
    LoginProcess StartSharedAuth(out Byte[] secret);

    /// <summary>
    /// Efetua login de uma entidade usando autenticação compartilhada.
    /// 
    /// A autenticação compartilhada é feita a partir de informações obtidas a
    /// através da operação 'StartSharedAuth' de uma conexão autenticada.
    /// </summary>
    /// <param name="login"> Objeto que represeta o processo de login iniciado.</param>
    /// <param name="secret"> Segredo a ser fornecido na conclusão do processo de login.</param>
    /// <exception cref="ArgumentException">Caso o login ou o segredo sejam nulos.</exception>
    /// <exception cref="AccessDenied"> O segredo fornecido não corresponde ao esperado
    /// pelo barramento.</exception>
    /// <exception cref="AlreadyLoggedInException"> A conexão já está autenticada.</exception>
    /// <exception cref="InvalidLoginProcessException"> O LoginProcess informado é inválido, por
    /// exemplo depois de ser cancelado ou ter expirado.</exception>
    /// <exception cref="ServiceFailure"> Ocorreu uma falha interna nos serviços
    /// do barramento que impediu a autenticação da conexão.</exception>
    void LoginBySharedAuth(LoginProcess login, Byte[] secret);

    /// <summary>
    /// Efetua logout da conexão, tornando o login atual inválido.
    /// 
    /// Após a chamada a essa operação a conexão fica desautenticada, implicando que
    /// qualquer chamada realizada pelo ORB usando essa conexão resultará numa
    /// exceção de sistema 'CORBA::NO_PERMISSION{NoLogin}' e chamadas recebidas
    /// por esse ORB serão respondidas com a exceção
    /// 'CORBA::NO_PERMISSION{UnknownBus}' indicando que não foi possível
    /// validar a chamada pois a conexão está temporariamente desautenticada.
    /// </summary>
    /// <returns>Verdadeiro se o processo de logout for concluído com êxito e 
    /// falso se não for possível invalidar o login atual.</returns>
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
  }
}