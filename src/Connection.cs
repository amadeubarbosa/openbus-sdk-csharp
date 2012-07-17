using System;
using omg.org.CORBA;
using tecgraf.openbus.core.v2_0.services;
using tecgraf.openbus.core.v2_0.services.access_control;
using tecgraf.openbus.core.v2_0.services.offer_registry;
using tecgraf.openbus.exceptions;

namespace tecgraf.openbus {
  public interface Connection {

    /*************************************************************************
     * ACESSO AO ORB *********************************************************
     *************************************************************************/

    ORB ORB { get; }

    /*************************************************************************
     * OBTENÇÃO DOS SERVICOS DO BARRAMENTO ***********************************
     *************************************************************************/

    OfferRegistry Offers { get; }

    /*************************************************************************
     * INFORMAÇÕES DO LOGIN **************************************************
     *************************************************************************/

    /// <summary>
    /// Barramento ao qual essa conexão se refere.
    /// </summary>
    string BusId { get; }

    /// <summary>
    /// Informações sobre o login da entidade que autenticou essa conexão.
    /// </summary>
    LoginInfo? Login { get; }

    /*************************************************************************
     * LOGIN AO BARRAMENTO ***************************************************
     *************************************************************************/
    /// <summary>
    /// Efetua login no barramento como uma entidade usando autenticação por
    /// senha.
    /// </summary>
    /// <param name="entity">Identificador da entidade a ser conectada.</param>
    /// <param name="password">Senha de autenticação da entidade no barramento.</param>
    /// <exception cref="AccessDenied"> A senha fornecida para autenticação da 
    /// entidade não foi validada pelo barramento.</exception>
    /// <exception cref="AlreadyLoggedInException">A conexão já está feita.</exception>
    /// <exception cref="ServiceFailure">Ocorreu uma falha interna nos serviços do
    /// barramento que impediu o estabelecimento da conexão.</exception>
    void LoginByPassword(String entity, Byte[] password);
 
    /// <summary>
    /// Efetua login no barramento como uma entidade usando autenticação por
    /// certificado.
    /// </summary>
    /// <param name="entity"> Identificador da entidade a ser conectada.</param>
    /// <param name="privKey"> Chave privada da entidade utilizada na autenticação.</param>
    /// <exception cref="MissingCertificate"> Não há certificado para essa entidade
    /// registrado no barramento indicado.</exception>
    /// <exception cref="CorruptedPrivateKeyException"> A chave privada fornecida está 
    /// corrompida.</exception>
    /// <exception cref="WrongPrivateKeyException"> A chave privada fornecida não 
    /// corresponde ao certificado da entidade registrado no barramento indicado.</exception>
    /// <exception cref="AlreadyLoggedInException"> A conexão já está logada.</exception>
    /// <exception cref="ServiceFailure"> Ocorreu uma falha interna nos serviços
    /// do barramento que impediu o estabelecimento da conexão.</exception>
    void LoginByCertificate(String entity, Byte[] privKey);

    /// <summary>
    /// Inicia o processo de login por autenticação compartilhada e cria um
    /// objeto para a conclusão desse processo.
    /// 
	  /// O objeto criado para conclusão do processo de login só pode ser utilizado
	  /// para concluir um único login. Após a conclusão do login (com sucesso ou
	  /// falha), o objeto fica inválido. O objeto criado também pode ficar
	  /// inválido após um tempo. Em ambos os casos, é necessário reiniciar o
	  /// processo de login por certificado chamando essa operação novamente.
    /// 
	  /// Retorna um objeto a ser usado para efetuar o login e um desafio.
    /// </summary>
    /// <exception cref="ServiceFailure"> Ocorreu uma falha interna nos serviços
    /// do barramento que impediu a obtenção do objeto de login e desafio.</exception>
    LoginProcess StartSharedAuth(out Byte[] secret);

    /// <summary>
    /// Efetua login no barramento como uma entidade usando autenticação por
    /// um outro login.
    /// </summary>
    /// <param name="login"> Objeto de login a ser utilizado.</param>
    /// <param name="secret"> Segredo decodificado a partir de outro login.</param>
	  /// <exception cref="InvalidLoginProcessException"> O LoginProcess informado é inválido,
	  /// por exemplo depois de ser cancelado ou ter expirado.</exception>
    /// <exception cref="WrongSecretException"> O segredo não corresponde ao esperado.</exception>
    /// <exception cref="AlreadyLoggedInException"> A conexão já está logada.</exception>
    /// <exception cref="ServiceFailure"> Ocorreu uma falha interna nos serviços
    /// do barramento que impediu o estabelecimento da conexão.</exception>
    void LoginBySharedAuth(LoginProcess login, Byte[] secret);

     
    /*************************************************************************
     * GERÊNCIA DO LOGIN *****************************************************
     *************************************************************************/

    /// <summary>
    /// Efetua logout no barramento.
    /// Retorna verdadeiro se o processo de logout for concluído com êxito e
    /// falso se a conexão já estiver deslogada.
    /// </summary>
    bool Logout();

    /// <summary>
    /// Objeto que implementa a interface de callback a ser
    /// chamada ou 'null' caso nenhum objeto deva ser chamado na
    /// ocorrência desse evento.
    /// </summary>
    InvalidLoginCallback OnInvalidLogin { get; set; }
     
    /*************************************************************************
     * INSPEÇÃO DO CLIENTE DAS CHAMADAS **************************************
     *************************************************************************/

    /// <summary>
    /// Caso a thread corrente seja a thread de execução de uma chamada remota
    /// oriunda do barramento dessa conexão, essa operação devolve um objeto que
    /// representa a cadeia de chamadas do barramento que esta chamada faz parte.
    /// Caso contrário devolve 'null'.
    /// </summary>
    CallerChain CallerChain { get; }
     
    /*************************************************************************
     * DELEGAÇÃO DE DIREITOS EM CHAMADAS *************************************
     *************************************************************************/
     
    /// <summary>
    /// Associa uma cadeia de chamadas do barramento a thread corrente, de forma
    /// que todas as chamadas remotas seguintes dessa thread através dessa
    /// conexão sejam feitas como parte dessa cadeia de chamadas.
    /// </summary>
    void JoinChain(CallerChain chain);
     
    /// <summary>
    /// Remove a associação da cadeia de chamadas com a thread corrente, fazendo
    /// com que todas as chamadas seguintes da thread corrente feitas através
    /// dessa conexão  deixem de fazer parte da cadeia de chamadas associada
    /// previamente. Ou seja, todas as chamadas passam a iniciar novas cadeias
    /// de chamada.
    /// </summary>
    void ExitChain();

    /// <summary>
    /// Devolve um objeto que representa a cadeia de chamadas associada a thread
    /// atual nessa conexão. A cadeia de chamadas informada foi associada
    /// previamente pela operação 'joinChain'. Caso a thread corrente não tenha
    /// nenhuma cadeia associada, essa operação devolve 'null'.
    /// </summary>
    CallerChain JoinedChain { get; }
   }
}
