using System;
using tecgraf.openbus.core.v2_00.services;
using tecgraf.openbus.core.v2_00.services.access_control;

namespace tecgraf.openbus.sdk {
  public interface IConnection {
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
    /// <exception cref="AlreadyLoggedIn">A conexão já está feita.</exception>
    /// <exception cref="ServiceFailure">Ocorreu uma falha interna nos serviços do
    /// barramento que impediu o estabelecimento da conexão.</exception>
    void LoginByPassword(String entity, Byte[] password);
 
    /// <summary>
    /// Efetua login no barramento como uma entidade usando autenticação por
    /// certificado.
    /// </summary>
    /// <param name="entity"> Identificador da entidade a ser conectada.</param>
    /// <param name="privateKey"> Chave privada da entidade utilizada na autenticação.</param>
    /// <exception cref="MissingCertificate"> Não há certificado para essa entidade
    /// registrado no barramento indicado.</exception>
    /// <exception cref="CorruptedPrivateKey"> A chave privada fornecida está 
    /// corrompida.</exception>
    /// <exception cref="WrongPrivateKey"> A chave privada fornecida não 
    /// corresponde ao certificado da entidade registrado no barramento indicado.</exception>
    /// <exception cref="AlreadyLoggedIn"> A conexão já está logada.</exception>
    /// <exception cref="ServiceFailure"> Ocorreu uma falha interna nos serviços
    /// do barramento que impediu o estabelecimento da conexão.</exception>
    void LoginByCertificate(String entity, Byte[] privateKey);
     
    /*************************************************************************
     * GERÊNCIA DO LOGIN *****************************************************
     *************************************************************************/

    /// <summary>
    /// Informa se a conexão está logada em um dado momento.
    /// </summary>
    bool IsLoggedIn();

    /// <summary>
    /// Efetua logout no barramento. Retorna verdadeiro se o processo de logout
    /// for concluído com êxito e falso se a conexão já estiver deslogada 
    /// (login inválido).
    /// </summary>
    bool Logout();
     
    /// <param name="callback"> Objeto que implementa a interface de callback a ser
    /// chamada ou 'null' caso nenhum objeto deva ser chamado na
    /// ocorrência desse evento.</param>
    void SetExpiredLoginCallback(IExpiredLoginCallback callback);
     
    /// <summary>
    /// Devolve a callback a ser chamada sempre que o login expira. Retorna um 
    /// objeto que implementa a interface de callback associado a esse
    /// evento ou 'null' caso nenhum objeto de callback tenha sido associado.
    /// </summary>
    IExpiredLoginCallback GetExpiredLoginCallback();
     
    /*************************************************************************
     * INSPEÇÃO DO CLIENTE DAS CHAMADAS **************************************
     *************************************************************************/
     
    /// <summary>
    /// Caso a thread corrente seja a thread de execução de uma chamada remota
    /// oriunda do barramento dessa conexão, essa operação devolve um objeto que
    /// representa a cadeia de chamadas do barramento que esta chamada faz parte.
    /// Caso contrário devolve 'null'.
    /// </summary>
    CallChain GetCallerChain();
     
    /*************************************************************************
     * DELEGAÇÃO DE DIREITOS EM CHAMADAS *************************************
     *************************************************************************/
     
    /// <summary>
    /// Associa uma cadeia de chamadas do barramento a thread corrente, de forma
    /// que todas as chamadas remotas seguintes dessa thread através dessa
    /// conexão sejam feitas como parte dessa cadeia de chamadas.
    /// </summary>
    void JoinChain(CallChain chain);
     
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
    CallChain GetJoinedChain();
     
    /*************************************************************************
     * GERÊNCIA DO CICLO DE VIDA DA CONEXÃO **********************************
     *************************************************************************/
     
    /// <summary>
    /// Encerra essa conexão, tornando-a inválida daqui em diante.
    /// </summary>
    void Close();
   }
}
