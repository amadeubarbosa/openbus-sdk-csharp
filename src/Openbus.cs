using System;
using System.Collections.Generic;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Ch.Elca.Iiop;
using Ch.Elca.Iiop.Idl;
using log4net;
using omg.org.CORBA;
using omg.org.PortableInterceptor;
using scs.core;
using tecgraf.openbus.core.v1_05.access_control_service;
using tecgraf.openbus.core.v1_05.registry_service;
using Tecgraf.Openbus.Exception;
using Tecgraf.Openbus.Interceptors;
using Tecgraf.Openbus.Lease;
using Tecgraf.Openbus.Security;


namespace Tecgraf.Openbus
{
  /// <summary>
  /// API de acesso a um barramento OpenBus.
  /// </summary>
  public class Openbus
  {
    #region Fields

    private static ILog logger = LogManager.GetLogger(typeof(Openbus));

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
    private OrbServices orb;
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
    private IiopChannel channel;

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
    /// Mantém a lista de métodos a serem liberados por interface
    /// no interceptador servidor. 
    /// </summary>
    private Dictionary<String, List<String>> interceptableMethods;

    /// <summary>
    /// Gerencia a lista de réplicas.
    /// </summary>
    private FaultToleranceManager ftManager;

    #endregion

    #region Consts

    /// <summary>
    /// Versão atual do Openbus
    /// </summary>
    internal const String OPENBUS_VERSION = "1_05";

    /// <summary>
    /// Representa a chave para obtenção do barramento.
    /// </summary>
    internal const String OPENBUS_KEY = "openbus_v" + OPENBUS_VERSION;

    /// <summary>
    /// Nome do receptáculo do Serviço de Registro.
    /// </summary>
    internal const String REGISTRY_SERVICE_RECEPTACLE_NAME = "RegistryServiceReceptacle";

    /// <summary>
    /// Conjunto de Object Keys do Serviço de Controle de Acesso.
    /// </summary>
    internal const String ACCESS_CONTROL_SERVICE_KEY = "ACS_v" + OPENBUS_VERSION;
    internal const String LEASE_PROVIDER_KEY = "LP_v" + OPENBUS_VERSION;
    internal const String FAULT_TOLERANT_ACS_KEY = "FTACS_v" + OPENBUS_VERSION;

    /// <summary>
    /// Object Keys do Serviço de Registro.
    /// </summary>
    internal const String REGISTRY_SERVICE_KEY = "RS_v" + OPENBUS_VERSION;

    /// <summary>
    /// Métodos remotos que podem ser lançados por um objeto CORBA.
    /// </summary>
    internal static readonly String[] CORBA_OBJECT_METHODS = new String[7] {
        "_interface",
        "_is_a",
        "_non_existent", 
        "_domain_managers",
        "_component",
        "_repository_id",
        "_get_policy"
    };
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
        logger.Error("O serviço de controle de acesso não foi encontrado");
        throw new ACSUnavailableException();
      }

      String acsID = Repository.GetRepositoryID(typeof(IAccessControlService));

      MarshalByRefObject acsObjRef;
      try {
        acsObjRef = this.acsComponent.getFacet(acsID);
      }
      catch (AbstractCORBASystemException e) {
        logger.Error("O serviço de controle de acesso não foi encontrado", e);
        throw new ACSUnavailableException();
      }

      if (acsObjRef == null) {
        logger.Error("O serviço de controle de acesso não foi encontrado");
        return;
      }
      this.acs = acsObjRef as IAccessControlService;

      String leaseProviderID = Repository.GetRepositoryID(typeof(ILeaseProvider));
      MarshalByRefObject lpObjRef = this.acsComponent.getFacet(leaseProviderID);
      if (lpObjRef == null) {
        logger.Error(
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

    #region Public Members

    /// <summary>
    /// Inicializa o Orb.
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
      Init(host, port, CredentialValidationPolicy.ALWAYS, true);
    }

    /// <summary>
    /// Inicializa o Orb.
    /// </summary>
    /// <param name="host">O endereço do serviço de controle de acesso.</param>
    /// <param name="port">A porta do serviço de controle de acesso.</param>
    /// <param name="ftConfigPath">O caminho para o arquivo de configuração do
    /// tolerância a falha</param>
    /// <param name="policy">A política de validação de credenciais obtidas pelo
    /// interceptador servidor.</param>
    /// <param name="hasServant">Indica se o Orb será inicializado permitindo
    /// prover servants.</param>
    /// <exception cref="OpenbusAlreadyInitialized"> Caso o Openbus já esteja
    /// inicializado.</exception>
    /// <exception cref="ArgumentException">Caso os argumentos estejam
    /// incorretos</exception>
    /// <exception cref="System.Security.SecurityException">Caso não possua
    /// permissão para configurar um canal.</exception>
    internal void Init(String host, int port, String ftConfigPath,
        CredentialValidationPolicy policy, Boolean hasServant) {
      if (String.IsNullOrEmpty(ftConfigPath))
        throw new ArgumentException(
            "O campo 'ftConfigPath' não pode ser nulo ou vazio.");

      this.ftManager = new FaultToleranceManager(ftConfigPath);
      Init(host, port, policy, hasServant);
    }

    /// <summary>
    /// Inicializa o Orb.
    /// </summary>
    /// <param name="host">O endereço do serviço de controle de acesso.</param>
    /// <param name="port">A porta do serviço de controle de acesso.</param>
    /// <param name="policy">A política de validação de credenciais obtidas pelo
    /// interceptador servidor.</param>
    /// <param name="hasServant">Indica se o Orb será inicializado permitindo
    /// prover servants.</param>
    /// <exception cref="OpenbusAlreadyInitialized"> Caso o Openbus já esteja
    /// inicializado.</exception>
    /// <exception cref="ArgumentException">Caso os argumentos estejam
    /// incorretos</exception>
    /// <exception cref="System.Security.SecurityException">Caso não possua
    /// permissão para configurar um canal.</exception>
    public void Init(String host, int port, CredentialValidationPolicy policy, Boolean hasServant) {
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
        this.orb.RegisterPortableInterceptorInitalizer(
            new ServerInitializer(policy));
        this.orb.CompleteInterceptorRegistration();
      }

      if (hasServant)
        this.channel = new IiopChannel(0);
      else
        this.channel = new IiopChannel();
      ChannelServices.RegisterChannel(channel, false);
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
        logger.Fatal(
          "O IComponent do AccessControlService não está disponível.");
        return null;
      }

      String receptaclesID = Repository.GetRepositoryID(typeof(IReceptacles));
      MarshalByRefObject rgsObjRef = this.acsComponent.getFacet(receptaclesID);
      if (rgsObjRef == null) {
        logger.Fatal("O Receptáculo " + receptaclesID + " está nulo.");
        return null;
      }

      IReceptacles receptacles = rgsObjRef as IReceptacles;
      ConnectionDescription[] connections = null;
      try {
        connections = receptacles.getConnections(REGISTRY_SERVICE_RECEPTACLE_NAME);
      }
      catch (InvalidName e) {
        logger.Error("Erro ao obter o RegistryServiceReceptacle.", e);
      }
      if (connections == null || connections.Length == 0) {
        logger.Fatal("O Serviço de Registro não está no ar.");
        return null;
      }
      if (connections.Length != 1)
        logger.Debug("Existe mais de um Serviço de Registro no ar.");

      MarshalByRefObject objRef = connections[0].objref;
      IComponent registryComponent = objRef as IComponent;
      if (registryComponent == null) {
        logger.Warn("Erro ao acessar a faceta 'IComponent' do Serviço de Registro");
        return null;
      }

      String registryServiceID = Repository.GetRepositoryID(typeof(IRegistryService));
      MarshalByRefObject objReg = registryComponent.getFacet(registryServiceID);
      this.registryService = objReg as IRegistryService;

      return this.registryService;
    }

    /// <summary>
    /// Fornece a credencial interceptada a partir da requisição atual.
    /// </summary>
    /// <returns></returns>
    public Credential GetInterceptedCredential() {
      omg.org.PortableInterceptor.Current pic = (omg.org.PortableInterceptor.Current)orb.resolve_initial_references("PICurrent");

      Object requestCredentialValue;
      try {
        requestCredentialValue = pic.get_slot(this.requestCredentialSlot);
      }
      catch (InvalidSlot e) {
        logger.Fatal("Erro ao obter a credencial interceptada.", e);
        return new Credential();
      }
      if (requestCredentialValue == null)
        return new Credential();

      return (Credential)requestCredentialValue;
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

      logger.Debug("Thread de renovação de lease está ativa. Lease = "
        + leaseTime + " segundos.");

      this.registryService = this.GetRegistryService();
      logger.Info(String.Format(
          "SDK conectado com o barramento: {0}:{1} e login '{2}'", host, port, user));
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
      catch (CryptographicException e) {
        throw new ACSLoginFailureException("Ocorreu um erro ao realizar a " +
            "autenticação no barramento. Verifique se a chave privada " +
            "utilizada corresponde ao certificado digital cadastrado.", e);
      }

      int leaseTime = -1;
      bool connect = this.acs.loginByCertificate(name, answer,
          out this.credential, out leaseTime);
      if (!connect) {
        throw new ACSLoginFailureException(
            "Não foi possível se conectar com o barramento.");
      }

      this.leaseRenewer = new LeaseRenewer(this.Credential, this.leaseProvider,
          new OpenbusExpiredCallback());
      this.leaseRenewer.Start();

      logger.Info("Thread de renovação de lease está ativa. Lease = "
          + leaseTime + " segundos.");
      this.registryService = GetRegistryService();

      logger.Info(String.Format(
          "SDK conectado com o barramento '{0}:{1}' e entidade '{2}' ", host,
          port, name));
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

      logger.Info(String.Format(
          "SDK conectado com o barramento '{0}:{1}'", host, port));
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
    /// Por padrão todos os métodos são interceptados.
    /// </summary>
    /// <param name="iface">RepID da interface</param>
    /// <param name="method">Nome do método.</param>
    /// <param name="interceptable">Indica se o método deve ser inteceptado 
    /// ou não.</param>
    private void SetInterceptable(String iface, String method,
      bool interceptable) {
      if ((String.IsNullOrEmpty(iface)) || (String.IsNullOrEmpty(method))) {
        logger.Error("Os parâmetros não podem ser vazios ou nulos.");
        return;
      }
      List<String> methods = null;
      if (interceptableMethods.ContainsKey(iface)) {
        methods = interceptableMethods[iface];
      }

      if (interceptable) {
        if (methods != null) {
          methods.Remove(method);
          if (methods.Count == 0) {
            interceptableMethods.Remove(iface);
          }
        }
      }
      else {
        if (methods == null) {
          methods = new List<string>();
          interceptableMethods.Add(iface, methods);
        }
        if (!methods.Contains(method))
          methods.Add(method);
      }
    }

    /// <summary>
    /// Indica se o método da interface dever interceptado.
    /// </summary>
    /// <param name="iface">RepID da interface.</param>
    /// <param name="method">Nome do método a ser testado.</param>        
    /// <returns><code>True</code> se o método de ver interceptado, caso 
    /// contrário <code>false</code>.</returns>
    private bool IsInterceptable(String iface, String method) {
      if (!interceptableMethods.ContainsKey(iface))
        return true;

      List<String> methods = interceptableMethods[iface];
      return !methods.Contains(method);
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
      logger.Debug("Atualizando estado do Openbus");
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
