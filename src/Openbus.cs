using System;
using Ch.Elca.Iiop;
using OpenbusAPI.Interceptors;
using System.Runtime.Remoting;
using OpenbusAPI.Lease;
using scs.core;
using System.Threading;
using Ch.Elca.Iiop.Idl;
using omg.org.CORBA;
using OpenbusAPI.Exception;
using System.Security.Cryptography.X509Certificates;
using OpenbusAPI.Security;
using System.Runtime.Remoting.Channels;
using OpenbusAPI.Logger;
using System.Security.Cryptography;
using tecgraf.openbus.core.v1_05.access_control_service;
using tecgraf.openbus.core.v1_05.registry_service;
using tecgraf.openbus.session_service.v1_05;
using System.Collections.Generic;


namespace OpenbusAPI
{
  /// <summary>
  /// API de acesso a um barramento OpenBus.
  /// </summary>
  public class Openbus
  {
    #region Fields

    /// <summary>
    /// O endereço do serviço de controle de acesso.
    /// </summary>
    private String host;
    /// <summary>
    /// Porta do serviço de controle de acesso.
    /// </summary>
    private int port;
    /// <summary>
    /// O Orb do IIOP.NET.
    /// <!-- Atenção: O orb é singleton, com isso só podemos registrar 
    /// interceptadores uma única vez. -->
    /// </summary>
    OrbServices orb;
    /// <summary>
    /// A faceta IAccessControlService do serviço de controle de acesso.
    /// </summary>
    private IAccessControlService acs;
    /// <summary>
    /// A faceta IComponent do serviço de controle de acesso.
    /// </summary>
    private IComponent acsComponent;
    /// <summary>
    /// A faceta ILeaseProvider do serviço de controle de acesso.
    /// </summary>
    private ILeaseProvider leaseProvider;

    /// <summary>
    /// O renovador de lease.
    /// </summary>
    private LeaseRenewer leaseRenewer;
    /// <summary>
    /// <i>Callback</i> para a notificação de que um <i>lease</i> expirou.
    /// </summary>
    private LeaseExpiredCallback leaseExpiredCb;

    /// <summary>
    /// O canal IIOP responsável pela troca de mensagens com o barramento.
    /// </summary>
    private IiopClientChannel channel;

    /// <summary>
    /// Credencial recebida ao se conectar ao barramento.
    /// </summary>
    private Credential credential;

    /// <summary>
    /// Credencial utilizada para trocar informações com o barramento.
    /// Esta credencial pode ser alterada pelo usuário para adicionar a
    /// delegação.
    /// </summary>
    public Credential Credential {
      get {
        return String.IsNullOrEmpty(currentCredential.identifier)
          ? credential : currentCredential;
      }
    }
    [ThreadStatic]
    static private Credential currentCredential;

    /// <summary>
    /// O slot da credencial da requisição.
    /// </summary>
    public int RequestCredentialSlot {
      set { requestCredentialSlot = value; }
    }
    private int requestCredentialSlot;

    /// <summary>
    /// A faceta IRegistryService do serviço de registro.
    /// </summary>
    private IRegistryService registryService;
    /// <summary>
    /// A faceta ISessionService do serviço de sessão
    /// </summary>
    private ISessionService sessionService;

    /// <summary>
    /// Mantém a lista de métodos a serem liberados no interceptador servidor.
    /// </summary>
    private Dictionary<String, List<String>> interceptableMethods;

    /// <summary>
    /// Gerencia a lista de réplicas.
    /// </summary>
    private FaultToleranceManager ftManager;

    #endregion

    #region Consts

    /// <summary>
    /// Representa a chave para obtenção do barramento.
    /// </summary>
    internal const String OPENBUS_KEY = "openbus_v1_05";

    /// <summary>
    /// Nome do receptáculo do Serviço de Registro.
    /// </summary>
    internal const String REGISTRY_SERVICE_RECEPTACLE_NAME = "RegistryServiceReceptacle";

    /// <summary>
    /// Conjunto de Object Keys do Serviço de Controle de Acesso.
    /// </summary>
    internal const String ACCESS_CONTROL_SERVICE_KEY = "ACS_v1_05";
    internal const String LEASE_PROVIDER_KEY = "LP_v1_05";
    internal const String FAULT_TOLERANT_ACS_KEY = "FTACS_v1_05";

    /// <summary>
    /// Conjunto de Object Keys do Serviço de Registro.
    /// </summary>
    internal const String REGISTRY_SERVICE_KEY = "RS_v1_05";

    #endregion

    #region Constructors

    /// <summary>
    /// Construtor privado da classe Openbus
    /// </summary>
    private static Openbus instance;
    private Openbus() {
      ResetInstance();
    }

    /// <summary>
    /// Recebe a instância do barramento.
    /// A classe Openbus é um singleton.
    /// </summary>
    /// <returns>O barramento</returns>
    public static Openbus GetInstance() {
      if (instance == null)
        instance = new Openbus();
      return instance;
    }

    #endregion

    #region Internal Members

    /// <summary>
    /// Reinicia o estado do barramento.
    /// </summary>
    private void Reset() {
      this.acs = null;
      this.acsComponent = null;
      this.leaseProvider = null;

      this.leaseRenewer = null;

      this.credential = new Credential();
      currentCredential = new Credential();
      this.requestCredentialSlot = -1;

      this.registryService = null;
      this.sessionService = null;
    }

    /// <summary>
    /// Retorna ao seu estado inicial, ou seja, desfaz as definições de
    /// atributos realizadas.
    /// </summary>
    private void ResetInstance() {
      this.channel = null;
      this.host = String.Empty;
      this.port = -1;
      this.leaseExpiredCb = null;
      this.interceptableMethods = new Dictionary<String, List<String>>();
      this.ftManager = null;
      Reset();
    }

    /// <summary>
    /// Se conecta ao AccessControlServer por meio do endereço e da porta.
    /// </summary>
    /// <exception cref="ACSUnavailableException"> Erro ao obter o serviço de 
    /// controle de acesso.</exception>
    internal void FetchACS() {
      this.acsComponent = RemotingServices.Connect(typeof(IComponent),
          "corbaloc::1.0@" + host + ":" + port + "/" + OPENBUS_KEY) as
          IComponent;

      if (this.acsComponent == null) {
        Log.COMMON.Error("O serviço de controle de acesso não foi encontrado");
        throw new ACSUnavailableException();
      }

      String acsID = Repository.GetRepositoryID(typeof(IAccessControlService));

      MarshalByRefObject acsObjRef;
      try {
        acsObjRef = this.acsComponent.getFacet(acsID);
      }
      catch (AbstractCORBASystemException e) {
        Log.COMMON.Error("O serviço de controle de acesso não foi encontrado", e);
        throw new ACSUnavailableException();
      }

      if (acsObjRef == null) {
        Log.COMMON.Error("O serviço de controle de acesso não foi encontrado");
        return;
      }
      this.acs = acsObjRef as IAccessControlService;

      String leaseProviderID = Repository.GetRepositoryID(typeof(ILeaseProvider));
      MarshalByRefObject lpObjRef = this.acsComponent.getFacet(leaseProviderID);
      if (lpObjRef == null) {
        Log.COMMON.Error(
          "O serviço de controle de acesso não possui a faceta ILeaseProvider");
        return;
      }
      this.leaseProvider = lpObjRef as ILeaseProvider;
    }

    /// <summary>
    /// Fornece o componente do Serviço de Controle de Acesso.
    /// </summary>
    /// <returns>
    /// A faceta IComponent do Serviço de Controle de Acesso.
    /// </returns>
    internal IComponent GetACSComponent() {
      return this.acsComponent;
    }

    /// <summary>
    /// Fornece a faceta ILeaseProvider do Serviço de Controle de Acesso.
    /// </summary>
    /// <returns>
    /// A faceta ILeaseProvider do Serviço de Controle de Acesso.
    /// </returns>
    internal ILeaseProvider GetLeaseProvider() {
      return this.leaseProvider;
    }

    /// <summary>
    /// Fornece o gerente de réplicas.
    /// </summary>
    /// <returns>O gerente de réplicas</returns>
    internal FaultToleranceManager GetFaultToleranceManager() {
      return this.ftManager;
    }

    /// <summary>
    /// Define o endereço do serviço de controle de acesso.
    /// </summary>
    /// <param name="host">O endereço do Serviço de Controle de Acesso.</param>
    /// <param name="port">A porta do Serviço de Controle de Acess.</param>
    internal void SetACSAdress(String host, int port) {
      this.host = host;
      this.port = port;
    }

    #endregion

    #region OpenbusAPI Implemented

    /// <summary>
    /// Retorna o barramento para o seu estado inicial, ou seja, desfaz as
    /// definições de atributos realizadas. Em seguida, inicializa o Orb.
    /// </summary>
    /// <param name="host">O endereço do serviço de controle de acesso.</param>
    /// <param name="port">A porta do serviço de controle de acesso.</param>
    /// <exception cref="OpenbusAlreadyInitialized"> Caso o Openbus já esteja
    /// inicializado.</exception>
    /// <exception cref="ArgumentException">Caso os argumentos estejam
    /// incorretos</exception>
    /// <exception cref="System.Security.SecurityException">Caso não possua
    /// permissão para configurar um canal.</exception>
    public void Init(String host, int port) {
      if (this.host != String.Empty)
        throw new OpenbusAlreadyInitialized();

      if (string.IsNullOrEmpty(host))
        throw new ArgumentException("O campo 'host' não é válido");
      if (port < 0)
        throw new ArgumentException("O campo 'port' não pode ser negativo.");

      this.host = host;
      this.port = port;

      bool isFaultTolerant = (this.ftManager != null);

      //Adicionando os interceptadores
      if (this.orb == null) {
        this.orb = OrbServices.GetSingleton();
        this.orb.RegisterPortableInterceptorInitalizer(
            new ClientInitializer(isFaultTolerant));
        //TODO Adicionar interceptador servidor.
        this.orb.CompleteInterceptorRegistration();
      }

      //Registrando um canal
      this.channel = new IiopClientChannel();
      ChannelServices.RegisterChannel(channel, false);
    }

    /// <summary>
    /// Retorna o barramento para o seu estado inicial, ou seja, desfaz as
    /// definições de atributos realizadas. Em seguida, inicializa o Orb.
    /// </summary>
    /// <param name="host">O endereço do serviço de controle de acesso.</param>
    /// <param name="port">A porta do serviço de controle de acesso.</param>
    /// <param name="ftConfigPath">O caminho para o arquivo de configuração do
    ///  tolerância a falha</param>
    /// <exception cref="OpenbusAlreadyInitialized"> Caso o Openbus já esteja
    /// inicializado.</exception>
    /// <exception cref="ArgumentException">Caso os argumentos estejam
    /// incorretos</exception>
    /// <exception cref="System.Security.SecurityException">Caso não possua
    /// permissão para configurar um canal.</exception>
    public void Init(String host, int port, String ftConfigPath) {
      if (String.IsNullOrEmpty(ftConfigPath))
        throw new ArgumentException(
            "O campo 'ftConfigPath' não pode ser nulo ou vazio.");

      this.ftManager = new FaultToleranceManager(ftConfigPath);
      Init(host, port);
    }

    /// <summary>
    /// Coloca o Openbus em modo de espera para receber requisições.
    /// </summary>
    public void Run() {
      Thread.Sleep(Timeout.Infinite);
    }

    /// <summary>
    /// Fornece o serviço de controle de acesso.
    /// </summary>
    /// <returns>A faceta IAccessControlService do serviço de controle 
    /// de acesso.</returns>
    public IAccessControlService GetAccessControlService() {
      return this.acs;
    }

    /// <summary>
    /// Fornece o serviço de registro.
    /// </summary>
    /// <returns>A faceta IRegistryService do serviço de registro.</returns>
    public IRegistryService GetRegistryService() {
      if (this.registryService != null)
        return this.registryService;

      if (this.acsComponent == null) {
        Log.COMMON.Fatal(
          "O IComponent do AccessControlService não está disponível.");
        return null;
      }

      String receptaclesID = Repository.GetRepositoryID(typeof(IReceptacles));
      MarshalByRefObject rgsObjRef = this.acsComponent.getFacet(receptaclesID);
      if (rgsObjRef == null) {
        Log.COMMON.Fatal("O Receptáculo " + receptaclesID + " está nulo.");
        return null;
      }

      IReceptacles receptacles = rgsObjRef as IReceptacles;
      ConnectionDescription[] connections = null;
      try {
        connections = receptacles.getConnections(REGISTRY_SERVICE_RECEPTACLE_NAME);
      }
      catch (InvalidName e) {
        Log.COMMON.Error("Erro ao obter o RegistryServiceReceptacle.", e);
      }
      if (connections == null || connections.Length == 0) {
        Log.COMMON.Fatal("Não existem conexões no receptáculo.");
        return null;
      }
      if (connections.Length != 1)
        Log.COMMON.Warn("Existe mais de um RegistryService conectado.");

      MarshalByRefObject objRef = connections[0].objref;
      IComponent registryComponent = objRef as IComponent;
      if (registryComponent == null) {
        Log.COMMON.Warn("A referência recebida não é um IComponent");
        return null;
      }

      String registryServiceID = Repository.GetRepositoryID(typeof(IRegistryService));
      MarshalByRefObject objReg = registryComponent.getFacet(registryServiceID);
      this.registryService = objReg as IRegistryService;

      return this.registryService;
    }

    /// <summary>
    /// Fornece o serviço de sessão
    /// </summary>
    /// <returns>A faceta ISessionService do serviço de sessão</returns>
    public ISessionService GetSessionService() {
      if (this.sessionService != null)
        return this.sessionService;

      IRegistryService registryService = this.GetRegistryService();
      if (registryService == null) {
        Log.COMMON.Fatal("Não foi possível acessar o RegistryService");
        return null;
      }
      String sessionServiceID = Repository.GetRepositoryID(
        typeof(ISessionService));
      String[] facets = new String[] { sessionServiceID };
      ServiceOffer[] offers = registryService.find(facets);

      if (offers.Length < 1) {
        Log.COMMON.Error("Não foi possível acessar o SessionService");
        return null;
      }
      if (offers.Length > 1)
        Log.COMMON.Warn("Existe mais de um " + sessionServiceID + " conectado.");

      IComponent component = offers[0].member;
      MarshalByRefObject ssObjRef = component.getFacet(sessionServiceID);
      if (ssObjRef == null)
        Log.COMMON.Error("Erro ao obter o SessionServiceReceptacle.");

      this.sessionService = ssObjRef as ISessionService;

      return this.sessionService;
    }

    /// <summary>
    /// Fornece a credencial interceptada a partir da requisição atual.
    /// </summary>
    /// <returns></returns>
    public Credential GetInterceptedCredential() {
      //TODO try
      omg.org.PortableInterceptor.Current pic = (omg.org.PortableInterceptor.Current)orb.resolve_initial_references("PICurrent");
      Any requestCredentialValue = (Any)pic.get_slot(this.requestCredentialSlot);
      if (requestCredentialValue.Type.kind().Equals(TCKind.tk_null)) {
        //Log
        return new Credential(); //TODO invalidCredential
      }

      /*
      catch (org.omg.CORBA.UserException e)
      {
         Log.COMMON.severe("Erro ao obter a credencial da requisição,", e);
         return InvalidTypes.CREDENTIAL;
      */

      //TODO -- Testar esse método. Verificar o que get_slot retorna. é um any mesmo?

      throw new System.Exception("método não implementado");
    }

    /// <summary>
    /// Realiza uma tentativa de conexão com o barramento (serviço de controle
    /// de acesso e o serviço de registro), via nome de usuário e senha.
    /// </summary>
    /// <param name="user">O nome do usuário.</param>
    /// <param name="password">A senha.</param>
    /// <returns>O serviço de registro.</returns>
    /// <exception cref="System.ArgumentException">Caso os argumentos estejam
    /// incorretos.</exception>
    /// <exception cref="ACSLoginFailureException">Falha ao tentar estabelecer
    /// conexão com o barramento.</exception>
    /// <exception cref="ACSUnavailableException"> Erro ao obter o serviço de
    /// controle de acesso.</exception>
    public IRegistryService Connect(String user, String password) {
      if ((String.IsNullOrEmpty(user)) || (String.IsNullOrEmpty(password)))
        throw new ArgumentException(
          "Os parâmetros 'user' e 'password' não podem ser nulos.");

      if (!String.IsNullOrEmpty(this.Credential.identifier))
        throw new ACSLoginFailureException("O barramento já está conectado.");

      FetchACS();
      int leaseTime = -1;
      bool ok = acs.loginByPassword(user, password, out this.credential,
        out leaseTime);
      if (!ok)
        throw new ACSLoginFailureException(
          "Não foi possível conectar ao barramento.");

      this.leaseRenewer = new LeaseRenewer(this.Credential, this.leaseProvider,
        new OpenbusExpiredCallback());
      this.leaseRenewer.Start();

      Log.COMMON.Debug("Thread de renovação de lease está ativa. Lease = "
        + leaseTime + " segundos.");

      this.registryService = this.GetRegistryService();
      return this.registryService;
    }

    /// <summary>
    /// Realiza uma tentativa de conexão com o barramento (serviço de controle
    /// de acesso e o serviço de registro), via certificado.
    /// </summary>
    /// <param name="name">O nome da entidade.</param>
    /// <param name="privateKey">A chave privada.</param>
    /// <param name="acsCertificate">O certificado do serviço de controle 
    /// de acesso.</param>
    /// <returns>O serviço de registro.</returns>
    /// <exception cref="System.ArgumentException">Caso os argumentos estejam
    /// incorretos.</exception>
    /// <exception cref="ACSLoginFailureException">Falha ao tentar estabelecer
    /// conexão com o barramento.</exception>
    /// <exception cref="ACSUnavailableException"> Erro ao obter o serviço de
    /// controle de acesso.</exception>
    public IRegistryService Connect(String name,
      RSACryptoServiceProvider privateKey, X509Certificate2 acsCertificate) {
      if ((String.IsNullOrEmpty(name)) || (acsCertificate == null) ||
        (privateKey == null))
        throw new ArgumentException("Nenhum parâmetro pode ser nulo.");

      if (!String.IsNullOrEmpty(this.Credential.identifier))
        throw new ACSLoginFailureException("O barramento já está conectado.");

      FetchACS();
      byte[] challenge = this.acs.getChallenge(name);
      if (challenge.Length == 0)
        throw new ACSLoginFailureException(String.Format("Não foi possível" +
            "realizar a autenticação no barramento. Provavelmente, a " +
            "entidade {0} não está cadastrada.", name));

      byte[] answer = new byte[0];
      try {
        answer = Crypto.GenerateAnswer(challenge, privateKey, acsCertificate);
      }
      catch (CryptographicException) {
        throw new ACSLoginFailureException("Ocorreu um erro ao realizar a " +
          "autenticação no barramento. Verifique se a chave privada " +
          "utilizada corresponde ao certificado digital cadastrado.");
      }

      int leaseTime = -1;
      bool connect = this.acs.loginByCertificate(name, answer,
        out this.credential, out leaseTime);
      if (!connect) {
        Log.SERVICES.Fatal("Não foi possível se conectar com o barramento.");
        return null;
      }


      this.leaseRenewer = new LeaseRenewer(this.Credential, this.leaseProvider,
        new OpenbusExpiredCallback());
      this.leaseRenewer.Start();

      Log.COMMON.Info("Thread de renovação de lease está ativa. Lease = "
        + leaseTime + " segundos.");
      this.registryService = GetRegistryService();
      return this.registryService;
    }

    /// <summary>
    /// Realiza uma tentativa de conexão com o barramento (serviço de controle
    /// de acesso e o serviço de registro).
    /// </summary>
    /// <param name="credential">A credencial</param>
    /// <returns>O serviço de registro.</returns>
    /// <exception cref="System.ArgumentException">Caso os argumentos estejam
    /// incorretos.</exception>
    /// <exception cref="InvalidCredentialException">Caso a credencial seja
    /// inválida.</exception>
    /// <exception cref="ACSUnavailableException"> Erro ao obter o serviço de
    /// controle de acesso.</exception>
    public IRegistryService Connect(Credential credential) {
      if (String.IsNullOrEmpty(credential.identifier))
        throw new ArgumentException(
          "O parâmetro 'credential' não pode ser nulo.");

      FetchACS();
      bool ok = this.acs.isValid(credential);
      if (!ok)
        throw new InvalidCredentialException();

      this.credential = credential;
      this.registryService = this.GetRegistryService();
      return this.registryService;
    }

    /// <summary>
    /// Desfaz a conexão.
    /// </summary>
    /// <returns><code>true</code> caso a conexão seja desfeita, ou 
    /// <code>false</code> se nenhuma conexão estiver ativa. </returns>
    public bool Disconnect() {
      bool status = false;
      if (String.IsNullOrEmpty(this.credential.identifier))
        return false;

      if (this.leaseRenewer != null) {
        this.leaseRenewer.Finish();
        this.leaseRenewer = null;
      }
      status = this.acs.logout(this.credential);
      if (status)
        Reset();

      return status;
    }

    /// <summary>
    /// Finaliza a utilização do barramento.
    /// </summary>
    public void Destroy() {
      ChannelServices.UnregisterChannel(channel);
      ResetInstance();
    }

    /// <summary>
    /// Controla se o método deve ou não ser interceptado pelo servidor.
    /// </summary>
    /// <param name="iface">RepID da interface</param>
    /// <param name="method">Nome do método.</param>
    /// <param name="interceptable">Indica se o método deve ser inteceptado 
    /// ou não.</param>
    private void SetInterceptable(String iface, String method,
      bool interceptable) {
      if ((String.IsNullOrEmpty(iface)) || (String.IsNullOrEmpty(method))) {
        Log.COMMON.Error("Os parâmetros não podem ser vazios ou nulos.");
        return;
      }

      if (!interceptable) {
        if (interceptableMethods.ContainsKey(iface)) {
          List<String> methods = interceptableMethods[iface];
          if (methods == null)
            methods = new List<String>();
          if (method.Contains(method))
            return;
          methods.Add(method);
        }
        else {
          List<String> methods = new List<String>();
          methods.Add(method);
          interceptableMethods.Add(iface, methods);
        }
      }
      else {
        List<String> methods = interceptableMethods[iface];
        if (methods != null)
          methods.Remove(method);
      }
    }

    /// <summary>
    /// Indica se o método da interface dever interceptado.
    /// </summary>
    /// <param name="iface">RepID da interface.</param>
    /// <param name="method">Nome do método a ser testado.</param>        
    /// <returns><code>True</code> se o método de ver interceptado, caso 
    /// contrário <code>false</code>.</returns>
    /// <summary>
    private bool IsInterceptable(String iface, String method) {
      List<String> methods = interceptableMethods[iface];
      return (methods == null) || !methods.Contains(method);
    }

    /// <summary>
    /// Define uma credencial a ser utilizada no lugar da credencial corrente.
    /// Útil para fornecer uma credencial com o campo delegate preenchido.
    /// </summary>
    /// <param name="credencial"> Credencial a ser usada nas requisições a 
    /// serem realizadas.</param>
    public void SetThreadCredential(Credential credencial) {
      currentCredential = credencial;
    }

    /// <summary>
    /// Informa o estado de conexão com o barramento.
    /// </summary>
    /// <returns><code>True</code> se estiver conectado ao barramento, caso 
    /// contrário <code>false</code>.</returns>
    public bool IsConnected() {
      return (!String.IsNullOrEmpty(this.credential.identifier));
    }

    /// <summary>
    /// Atribui o observador para receber eventos de expiração do <i>lease</i>.
    /// </summary>
    /// <param name="leaseExpiredCallback">O observador.</param>
    public void SetLeaseExpiredCallback(LeaseExpiredCallback leaseExpiredCallback) {
      this.leaseExpiredCb = leaseExpiredCallback;
    }

    /// <summary>
    /// Remove o observador de expiração de <i>lease</i>.
    /// </summary>
    public void RemoveLeaseExpiredCallback() {
      this.leaseExpiredCb = null;
    }

    /// <summary>
    /// Informa aos observadores que o <i>lease</i> expirou.
    /// </summary>
    internal void LeaseExpired() {
      Log.LEASE.Debug("Atualizando estado do Openbus");
      Reset();
      if (this.leaseExpiredCb != null) {
        this.leaseExpiredCb.Expired();
      }
    }

    #endregion
  }

  #region Internal Class

  internal class OpenbusExpiredCallback : LeaseExpiredCallback
  {

    #region LeaseExpiredCallback Members

    /// <summary>
    /// Classe responsável por informar aos observadores que o <i>lease</i>
    /// expirou.
    /// </summary>
    public void Expired() {
      Openbus openbus = Openbus.GetInstance();
      openbus.LeaseExpired();
    }

    #endregion
  }

  #endregion

}
