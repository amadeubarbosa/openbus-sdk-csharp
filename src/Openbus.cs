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
    /// O endere�o do servi�o de controle de acesso.
    /// </summary>
    private String host;
    /// <summary>
    /// Porta do servi�o de controle de acesso.
    /// </summary>
    private int port;
    /// <summary>
    /// O Orb do IIOP.NET.
    /// <!-- Aten��o: O orb � singleton, com isso s� podemos registrar 
    /// interceptadores uma �nica vez. -->
    /// </summary>
    OrbServices orb;
    /// <summary>
    /// A faceta IAccessControlService do servi�o de controle de acesso.
    /// </summary>
    private IAccessControlService acs;
    /// <summary>
    /// A faceta IComponent do servi�o de controle de acesso.
    /// </summary>
    private IComponent acsComponent;
    /// <summary>
    /// A faceta ILeaseProvider do servi�o de controle de acesso.
    /// </summary>
    private ILeaseProvider leaseProvider;

    /// <summary>
    /// O renovador de lease.
    /// </summary>
    private LeaseRenewer leaseRenewer;
    /// <summary>
    /// <i>Callback</i> para a notifica��o de que um <i>lease</i> expirou.
    /// </summary>
    private LeaseExpiredCallback leaseExpiredCb;

    /// <summary>
    /// O canal IIOP respons�vel pela troca de mensagens com o barramento.
    /// </summary>
    private IiopClientChannel channel;

    /// <summary>
    /// Credencial recebida ao se conectar ao barramento.
    /// </summary>
    private Credential credential;

    /// <summary>
    /// Credencial utilizada para trocar informa��es com o barramento.
    /// Esta credencial pode ser alterada pelo usu�rio para adicionar a
    /// delega��o.
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
    /// O slot da credencial da requisi��o.
    /// </summary>
    public int RequestCredentialSlot {
      set { requestCredentialSlot = value; }
    }
    private int requestCredentialSlot;

    /// <summary>
    /// A faceta IRegistryService do servi�o de registro.
    /// </summary>
    private IRegistryService registryService;
    /// <summary>
    /// A faceta ISessionService do servi�o de sess�o
    /// </summary>
    private ISessionService sessionService;

    /// <summary>
    /// Mant�m a lista de m�todos a serem liberados no interceptador servidor.
    /// </summary>
    private Dictionary<String, List<String>> interceptableMethods;

    /// <summary>
    /// Gerencia a lista de r�plicas.
    /// </summary>
    private FaultToleranceManager ftManager;

    #endregion

    #region Consts

    /// <summary>
    /// Representa a chave para obten��o do barramento.
    /// </summary>
    internal const String OPENBUS_KEY = "openbus_v1_05";

    /// <summary>
    /// Nome do recept�culo do Servi�o de Registro.
    /// </summary>
    internal const String REGISTRY_SERVICE_RECEPTACLE_NAME = "RegistryServiceReceptacle";

    /// <summary>
    /// Conjunto de Object Keys do Servi�o de Controle de Acesso.
    /// </summary>
    internal const String ACCESS_CONTROL_SERVICE_KEY = "ACS_v1_05";
    internal const String LEASE_PROVIDER_KEY = "LP_v1_05";
    internal const String FAULT_TOLERANT_ACS_KEY = "FTACS_v1_05";

    /// <summary>
    /// Conjunto de Object Keys do Servi�o de Registro.
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
    /// Recebe a inst�ncia do barramento.
    /// A classe Openbus � um singleton.
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
    /// Retorna ao seu estado inicial, ou seja, desfaz as defini��es de
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
    /// Se conecta ao AccessControlServer por meio do endere�o e da porta.
    /// </summary>
    /// <exception cref="ACSUnavailableException"> Erro ao obter o servi�o de 
    /// controle de acesso.</exception>
    internal void FetchACS() {
      this.acsComponent = RemotingServices.Connect(typeof(IComponent),
          "corbaloc::1.0@" + host + ":" + port + "/" + OPENBUS_KEY) as
          IComponent;

      if (this.acsComponent == null) {
        Log.COMMON.Error("O servi�o de controle de acesso n�o foi encontrado");
        throw new ACSUnavailableException();
      }

      String acsID = Repository.GetRepositoryID(typeof(IAccessControlService));

      MarshalByRefObject acsObjRef;
      try {
        acsObjRef = this.acsComponent.getFacet(acsID);
      }
      catch (AbstractCORBASystemException e) {
        Log.COMMON.Error("O servi�o de controle de acesso n�o foi encontrado", e);
        throw new ACSUnavailableException();
      }

      if (acsObjRef == null) {
        Log.COMMON.Error("O servi�o de controle de acesso n�o foi encontrado");
        return;
      }
      this.acs = acsObjRef as IAccessControlService;

      String leaseProviderID = Repository.GetRepositoryID(typeof(ILeaseProvider));
      MarshalByRefObject lpObjRef = this.acsComponent.getFacet(leaseProviderID);
      if (lpObjRef == null) {
        Log.COMMON.Error(
          "O servi�o de controle de acesso n�o possui a faceta ILeaseProvider");
        return;
      }
      this.leaseProvider = lpObjRef as ILeaseProvider;
    }

    /// <summary>
    /// Fornece o componente do Servi�o de Controle de Acesso.
    /// </summary>
    /// <returns>
    /// A faceta IComponent do Servi�o de Controle de Acesso.
    /// </returns>
    internal IComponent GetACSComponent() {
      return this.acsComponent;
    }

    /// <summary>
    /// Fornece a faceta ILeaseProvider do Servi�o de Controle de Acesso.
    /// </summary>
    /// <returns>
    /// A faceta ILeaseProvider do Servi�o de Controle de Acesso.
    /// </returns>
    internal ILeaseProvider GetLeaseProvider() {
      return this.leaseProvider;
    }

    /// <summary>
    /// Fornece o gerente de r�plicas.
    /// </summary>
    /// <returns>O gerente de r�plicas</returns>
    internal FaultToleranceManager GetFaultToleranceManager() {
      return this.ftManager;
    }

    /// <summary>
    /// Define o endere�o do servi�o de controle de acesso.
    /// </summary>
    /// <param name="host">O endere�o do Servi�o de Controle de Acesso.</param>
    /// <param name="port">A porta do Servi�o de Controle de Acess.</param>
    internal void SetACSAdress(String host, int port) {
      this.host = host;
      this.port = port;
    }

    #endregion

    #region OpenbusAPI Implemented

    /// <summary>
    /// Retorna o barramento para o seu estado inicial, ou seja, desfaz as
    /// defini��es de atributos realizadas. Em seguida, inicializa o Orb.
    /// </summary>
    /// <param name="host">O endere�o do servi�o de controle de acesso.</param>
    /// <param name="port">A porta do servi�o de controle de acesso.</param>
    /// <exception cref="OpenbusAlreadyInitialized"> Caso o Openbus j� esteja
    /// inicializado.</exception>
    /// <exception cref="ArgumentException">Caso os argumentos estejam
    /// incorretos</exception>
    /// <exception cref="System.Security.SecurityException">Caso n�o possua
    /// permiss�o para configurar um canal.</exception>
    public void Init(String host, int port) {
      if (this.host != String.Empty)
        throw new OpenbusAlreadyInitialized();

      if (string.IsNullOrEmpty(host))
        throw new ArgumentException("O campo 'host' n�o � v�lido");
      if (port < 0)
        throw new ArgumentException("O campo 'port' n�o pode ser negativo.");

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
    /// defini��es de atributos realizadas. Em seguida, inicializa o Orb.
    /// </summary>
    /// <param name="host">O endere�o do servi�o de controle de acesso.</param>
    /// <param name="port">A porta do servi�o de controle de acesso.</param>
    /// <param name="ftConfigPath">O caminho para o arquivo de configura��o do
    ///  toler�ncia a falha</param>
    /// <exception cref="OpenbusAlreadyInitialized"> Caso o Openbus j� esteja
    /// inicializado.</exception>
    /// <exception cref="ArgumentException">Caso os argumentos estejam
    /// incorretos</exception>
    /// <exception cref="System.Security.SecurityException">Caso n�o possua
    /// permiss�o para configurar um canal.</exception>
    public void Init(String host, int port, String ftConfigPath) {
      if (String.IsNullOrEmpty(ftConfigPath))
        throw new ArgumentException(
            "O campo 'ftConfigPath' n�o pode ser nulo ou vazio.");

      this.ftManager = new FaultToleranceManager(ftConfigPath);
      Init(host, port);
    }

    /// <summary>
    /// Coloca o Openbus em modo de espera para receber requisi��es.
    /// </summary>
    public void Run() {
      Thread.Sleep(Timeout.Infinite);
    }

    /// <summary>
    /// Fornece o servi�o de controle de acesso.
    /// </summary>
    /// <returns>A faceta IAccessControlService do servi�o de controle 
    /// de acesso.</returns>
    public IAccessControlService GetAccessControlService() {
      return this.acs;
    }

    /// <summary>
    /// Fornece o servi�o de registro.
    /// </summary>
    /// <returns>A faceta IRegistryService do servi�o de registro.</returns>
    public IRegistryService GetRegistryService() {
      if (this.registryService != null)
        return this.registryService;

      if (this.acsComponent == null) {
        Log.COMMON.Fatal(
          "O IComponent do AccessControlService n�o est� dispon�vel.");
        return null;
      }

      String receptaclesID = Repository.GetRepositoryID(typeof(IReceptacles));
      MarshalByRefObject rgsObjRef = this.acsComponent.getFacet(receptaclesID);
      if (rgsObjRef == null) {
        Log.COMMON.Fatal("O Recept�culo " + receptaclesID + " est� nulo.");
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
        Log.COMMON.Fatal("N�o existem conex�es no recept�culo.");
        return null;
      }
      if (connections.Length != 1)
        Log.COMMON.Warn("Existe mais de um RegistryService conectado.");

      MarshalByRefObject objRef = connections[0].objref;
      IComponent registryComponent = objRef as IComponent;
      if (registryComponent == null) {
        Log.COMMON.Warn("A refer�ncia recebida n�o � um IComponent");
        return null;
      }

      String registryServiceID = Repository.GetRepositoryID(typeof(IRegistryService));
      MarshalByRefObject objReg = registryComponent.getFacet(registryServiceID);
      this.registryService = objReg as IRegistryService;

      return this.registryService;
    }

    /// <summary>
    /// Fornece o servi�o de sess�o
    /// </summary>
    /// <returns>A faceta ISessionService do servi�o de sess�o</returns>
    public ISessionService GetSessionService() {
      if (this.sessionService != null)
        return this.sessionService;

      IRegistryService registryService = this.GetRegistryService();
      if (registryService == null) {
        Log.COMMON.Fatal("N�o foi poss�vel acessar o RegistryService");
        return null;
      }
      String sessionServiceID = Repository.GetRepositoryID(
        typeof(ISessionService));
      String[] facets = new String[] { sessionServiceID };
      ServiceOffer[] offers = registryService.find(facets);

      if (offers.Length < 1) {
        Log.COMMON.Error("N�o foi poss�vel acessar o SessionService");
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
    /// Fornece a credencial interceptada a partir da requisi��o atual.
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
         Log.COMMON.severe("Erro ao obter a credencial da requisi��o,", e);
         return InvalidTypes.CREDENTIAL;
      */

      //TODO -- Testar esse m�todo. Verificar o que get_slot retorna. � um any mesmo?

      throw new System.Exception("m�todo n�o implementado");
    }

    /// <summary>
    /// Realiza uma tentativa de conex�o com o barramento (servi�o de controle
    /// de acesso e o servi�o de registro), via nome de usu�rio e senha.
    /// </summary>
    /// <param name="user">O nome do usu�rio.</param>
    /// <param name="password">A senha.</param>
    /// <returns>O servi�o de registro.</returns>
    /// <exception cref="System.ArgumentException">Caso os argumentos estejam
    /// incorretos.</exception>
    /// <exception cref="ACSLoginFailureException">Falha ao tentar estabelecer
    /// conex�o com o barramento.</exception>
    /// <exception cref="ACSUnavailableException"> Erro ao obter o servi�o de
    /// controle de acesso.</exception>
    public IRegistryService Connect(String user, String password) {
      if ((String.IsNullOrEmpty(user)) || (String.IsNullOrEmpty(password)))
        throw new ArgumentException(
          "Os par�metros 'user' e 'password' n�o podem ser nulos.");

      if (!String.IsNullOrEmpty(this.Credential.identifier))
        throw new ACSLoginFailureException("O barramento j� est� conectado.");

      FetchACS();
      int leaseTime = -1;
      bool ok = acs.loginByPassword(user, password, out this.credential,
        out leaseTime);
      if (!ok)
        throw new ACSLoginFailureException(
          "N�o foi poss�vel conectar ao barramento.");

      this.leaseRenewer = new LeaseRenewer(this.Credential, this.leaseProvider,
        new OpenbusExpiredCallback());
      this.leaseRenewer.Start();

      Log.COMMON.Debug("Thread de renova��o de lease est� ativa. Lease = "
        + leaseTime + " segundos.");

      this.registryService = this.GetRegistryService();
      return this.registryService;
    }

    /// <summary>
    /// Realiza uma tentativa de conex�o com o barramento (servi�o de controle
    /// de acesso e o servi�o de registro), via certificado.
    /// </summary>
    /// <param name="name">O nome da entidade.</param>
    /// <param name="privateKey">A chave privada.</param>
    /// <param name="acsCertificate">O certificado do servi�o de controle 
    /// de acesso.</param>
    /// <returns>O servi�o de registro.</returns>
    /// <exception cref="System.ArgumentException">Caso os argumentos estejam
    /// incorretos.</exception>
    /// <exception cref="ACSLoginFailureException">Falha ao tentar estabelecer
    /// conex�o com o barramento.</exception>
    /// <exception cref="ACSUnavailableException"> Erro ao obter o servi�o de
    /// controle de acesso.</exception>
    public IRegistryService Connect(String name,
      RSACryptoServiceProvider privateKey, X509Certificate2 acsCertificate) {
      if ((String.IsNullOrEmpty(name)) || (acsCertificate == null) ||
        (privateKey == null))
        throw new ArgumentException("Nenhum par�metro pode ser nulo.");

      if (!String.IsNullOrEmpty(this.Credential.identifier))
        throw new ACSLoginFailureException("O barramento j� est� conectado.");

      FetchACS();
      byte[] challenge = this.acs.getChallenge(name);
      if (challenge.Length == 0)
        throw new ACSLoginFailureException(String.Format("N�o foi poss�vel" +
            "realizar a autentica��o no barramento. Provavelmente, a " +
            "entidade {0} n�o est� cadastrada.", name));

      byte[] answer = new byte[0];
      try {
        answer = Crypto.GenerateAnswer(challenge, privateKey, acsCertificate);
      }
      catch (CryptographicException) {
        throw new ACSLoginFailureException("Ocorreu um erro ao realizar a " +
          "autentica��o no barramento. Verifique se a chave privada " +
          "utilizada corresponde ao certificado digital cadastrado.");
      }

      int leaseTime = -1;
      bool connect = this.acs.loginByCertificate(name, answer,
        out this.credential, out leaseTime);
      if (!connect) {
        Log.SERVICES.Fatal("N�o foi poss�vel se conectar com o barramento.");
        return null;
      }


      this.leaseRenewer = new LeaseRenewer(this.Credential, this.leaseProvider,
        new OpenbusExpiredCallback());
      this.leaseRenewer.Start();

      Log.COMMON.Info("Thread de renova��o de lease est� ativa. Lease = "
        + leaseTime + " segundos.");
      this.registryService = GetRegistryService();
      return this.registryService;
    }

    /// <summary>
    /// Realiza uma tentativa de conex�o com o barramento (servi�o de controle
    /// de acesso e o servi�o de registro).
    /// </summary>
    /// <param name="credential">A credencial</param>
    /// <returns>O servi�o de registro.</returns>
    /// <exception cref="System.ArgumentException">Caso os argumentos estejam
    /// incorretos.</exception>
    /// <exception cref="InvalidCredentialException">Caso a credencial seja
    /// inv�lida.</exception>
    /// <exception cref="ACSUnavailableException"> Erro ao obter o servi�o de
    /// controle de acesso.</exception>
    public IRegistryService Connect(Credential credential) {
      if (String.IsNullOrEmpty(credential.identifier))
        throw new ArgumentException(
          "O par�metro 'credential' n�o pode ser nulo.");

      FetchACS();
      bool ok = this.acs.isValid(credential);
      if (!ok)
        throw new InvalidCredentialException();

      this.credential = credential;
      this.registryService = this.GetRegistryService();
      return this.registryService;
    }

    /// <summary>
    /// Desfaz a conex�o.
    /// </summary>
    /// <returns><code>true</code> caso a conex�o seja desfeita, ou 
    /// <code>false</code> se nenhuma conex�o estiver ativa. </returns>
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
    /// Finaliza a utiliza��o do barramento.
    /// </summary>
    public void Destroy() {
      ChannelServices.UnregisterChannel(channel);
      ResetInstance();
    }

    /// <summary>
    /// Controla se o m�todo deve ou n�o ser interceptado pelo servidor.
    /// </summary>
    /// <param name="iface">RepID da interface</param>
    /// <param name="method">Nome do m�todo.</param>
    /// <param name="interceptable">Indica se o m�todo deve ser inteceptado 
    /// ou n�o.</param>
    private void SetInterceptable(String iface, String method,
      bool interceptable) {
      if ((String.IsNullOrEmpty(iface)) || (String.IsNullOrEmpty(method))) {
        Log.COMMON.Error("Os par�metros n�o podem ser vazios ou nulos.");
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
    /// Indica se o m�todo da interface dever interceptado.
    /// </summary>
    /// <param name="iface">RepID da interface.</param>
    /// <param name="method">Nome do m�todo a ser testado.</param>        
    /// <returns><code>True</code> se o m�todo de ver interceptado, caso 
    /// contr�rio <code>false</code>.</returns>
    /// <summary>
    private bool IsInterceptable(String iface, String method) {
      List<String> methods = interceptableMethods[iface];
      return (methods == null) || !methods.Contains(method);
    }

    /// <summary>
    /// Define uma credencial a ser utilizada no lugar da credencial corrente.
    /// �til para fornecer uma credencial com o campo delegate preenchido.
    /// </summary>
    /// <param name="credencial"> Credencial a ser usada nas requisi��es a 
    /// serem realizadas.</param>
    public void SetThreadCredential(Credential credencial) {
      currentCredential = credencial;
    }

    /// <summary>
    /// Informa o estado de conex�o com o barramento.
    /// </summary>
    /// <returns><code>True</code> se estiver conectado ao barramento, caso 
    /// contr�rio <code>false</code>.</returns>
    public bool IsConnected() {
      return (!String.IsNullOrEmpty(this.credential.identifier));
    }

    /// <summary>
    /// Atribui o observador para receber eventos de expira��o do <i>lease</i>.
    /// </summary>
    /// <param name="leaseExpiredCallback">O observador.</param>
    public void SetLeaseExpiredCallback(LeaseExpiredCallback leaseExpiredCallback) {
      this.leaseExpiredCb = leaseExpiredCallback;
    }

    /// <summary>
    /// Remove o observador de expira��o de <i>lease</i>.
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
    /// Classe respons�vel por informar aos observadores que o <i>lease</i>
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
