using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting;
using System.Security.Cryptography;
using System.Threading;
using Ch.Elca.Iiop.Idl;
using Org.BouncyCastle.Crypto;
using log4net;
using omg.org.CORBA;
using omg.org.IOP;
using omg.org.PortableInterceptor;
using scs.core;
using tecgraf.openbus.core.v1_05.access_control_service;
using tecgraf.openbus.core.v2_0;
using tecgraf.openbus.core.v2_0.credential;
using tecgraf.openbus.core.v2_0.services;
using tecgraf.openbus.core.v2_0.services.access_control;
using tecgraf.openbus.core.v2_0.services.offer_registry;
using tecgraf.openbus.caches;
using tecgraf.openbus.exceptions;
using tecgraf.openbus.interceptors;
using tecgraf.openbus.lease;
using tecgraf.openbus.security;
using Current = omg.org.PortableInterceptor.Current;
using TypeCode = omg.org.CORBA.TypeCode;

namespace tecgraf.openbus {
  internal class ConnectionImpl : Connection {
    #region Fields

    private static readonly ILog Logger =
      LogManager.GetLogger(typeof (ConnectionImpl));

    private readonly string _host;
    private readonly short _port;
    private readonly IComponent _acsComponent;
    private AccessControl _acs;
    private string _busId;
    private AsymmetricKeyParameter _busKey;
    private LeaseRenewer _leaseRenewer;

    internal bool Legacy;
    internal readonly ConnectionManagerImpl Manager;

    private volatile int _sessionId;
    private readonly object _sessionIdLock = new object();
    private readonly LoginStatus _login;
    private readonly object _loginLock = new object();
    // client caches
    private readonly LRUConcurrentDictionaryCache<EffectiveProfile, string>
      _profile2Login;

    private readonly LRUConcurrentDictionaryCache<string, ClientSideSession>
      _outgoingLogin2Session;

    private readonly ConditionalWeakTable<Thread, CallerChain> _joinedChainOf;

    // server caches
    private readonly LRUConcurrentDictionaryCache<int, ServerSideSession>
      _sessionId2Session;

    private LoginCache _loginsCache;

    /// <summary>
    /// Representam a identificação dos "service contexts" (contextos) utilizados
    /// para transporte de credenciais em requisições de serviço.
    /// </summary>
    private const int ContextId = CredentialContextId.ConstVal;

    private const int PrevContextId = 1234;

    private const byte MajorVersion = core.v2_0.MajorVersion.ConstVal;
    private const byte MinorVersion = core.v2_0.MinorVersion.ConstVal;
    private const int SecretSize = 16;

    private readonly Codec _codec;

    private readonly int _credentialSlotId;
    private readonly int _connectionSlotId;
    private readonly int _loginSlotId;

    #endregion

    #region Constructors

    internal ConnectionImpl(string host, short port,
                            ConnectionManagerImpl manager, bool legacy) {
      if (string.IsNullOrEmpty(host)) {
        throw new ArgumentException("O campo 'host' não é válido");
      }
      if (port < 0) {
        throw new ArgumentException("O campo 'port' não pode ser negativo.");
      }
      _host = host;
      _port = port;
      ORB = OrbServices.GetSingleton();
      Manager = manager;
      Legacy = legacy;
      _codec = ServerInterceptor.Instance.Codec;
      _credentialSlotId = ServerInterceptor.Instance.CredentialSlotId;
      _connectionSlotId = ServerInterceptor.Instance.ConnectionSlotId;
      _loginSlotId = ClientInterceptor.Instance.LoginSlotId;
      _acsComponent = RemotingServices.Connect(
        typeof (IComponent),
        "corbaloc::1.0@" + _host + ":" + _port + "/" + BusObjectKey.ConstVal)
                      as IComponent;
      if (_acsComponent == null) {
        throw new OpenBusException(
          "Não foi possível conectar ao barramento com o host e porta fornecidos.");
      }

      InternalKey = Crypto.GenerateKeyPair();

      _sessionId2Session =
        new LRUConcurrentDictionaryCache<int, ServerSideSession>();
      _profile2Login =
        new LRUConcurrentDictionaryCache<EffectiveProfile, string>();
      _outgoingLogin2Session =
        new LRUConcurrentDictionaryCache<String, ClientSideSession>();
      _joinedChainOf = new ConditionalWeakTable<Thread, CallerChain>();

      _login = new LoginStatus(this);
      EraseBusIdAndKey();
      GetBusFacets();
    }

    #endregion

    #region Internal Members

    private Current GetPICurrent() {
      Current current = ORB.resolve_initial_references("PICurrent") as Current;
      if (current == null) {
        const string message =
          "Falha inesperada ao acessar o slot da thread corrente";
        Logger.Fatal(message);
        throw new OpenBusException(message);
      }
      return current;
    }

    private void GetBusFacets() {
      string acsId = Repository.GetRepositoryID(typeof (AccessControl));
      string lrId = Repository.GetRepositoryID(typeof (LoginRegistry));
      string orId = Repository.GetRepositoryID(typeof (OfferRegistry));

      MarshalByRefObject acsObjRef = _acsComponent.getFacet(acsId);
      MarshalByRefObject lrObjRef = _acsComponent.getFacet(lrId);
      MarshalByRefObject orObjRef = _acsComponent.getFacet(orId);

      _acs = acsObjRef as AccessControl;
      LoginRegistry = lrObjRef as LoginRegistry;
      Offers = orObjRef as OfferRegistry;
      if ((_acs == null) || (LoginRegistry == null) || (Offers == null)) {
        Logger.Error("O serviço de controle de acesso não foi encontrado.");
        return;
      }

      if (Legacy) {
        try {
          IComponent legacy = RemotingServices.Connect(
            typeof (IComponent),
            "corbaloc::1.0@" + _host + ":" + _port + "/" + "openbus_v1_05") as
                              IComponent;
          string legacyId =
            Repository.GetRepositoryID(typeof (IAccessControlService));
          if (legacy == null) {
            Legacy = false;
            Logger.Error(
              "O serviço de controle de acesso 1.5 não foi encontrado. O suporte a conexões legadas foi desabilitado.");
          }
          else {
            MarshalByRefObject legacyObjRef = legacy.getFacet(legacyId);
            LegacyAccess = legacyObjRef as IAccessControlService;
            if (LegacyAccess == null) {
              Legacy = false;
              Logger.Error(
                "A faceta IAccessControlService do serviço de controle de acesso 1.5 não foi encontrada. O suporte a conexões legadas foi desabilitado.");
            }
          }
        }
        catch (Exception e) {
          Legacy = false;
          Logger.Error(
            "Erro ao tentar obter a faceta IAccessControlService da versão 1.5. O suporte a conexões legadas foi desabilitado.",
            e);
        }
      }
    }

    private string GetBusIdAndKey(out AsymmetricKeyParameter key) {
      key = Crypto.CreatePublicKeyFromBytes(_acs.buskey);
      return _acs.busid;
    }

    private void EraseBusIdAndKey() {
      lock (_loginLock) {
        BusId = null;
        _busKey = null;
      }
    }

    private AsymmetricCipherKeyPair InternalKey { get; set; }

    private void LoginByObject(LoginProcess login, byte[] secret) {
      if (_login.IsLoggedIn()) {
        login.cancel();
        throw new AlreadyLoggedInException();
      }

      AsymmetricKeyParameter key;
      string busId = GetBusIdAndKey(out key);

      byte[] encrypted;
      byte[] pubBytes = Crypto.GetPublicKeyInBytes(InternalKey.Public);
      try {
        //encode answer and hash of public key
        LoginAuthenticationInfo info =
          new LoginAuthenticationInfo
          {data = secret, hash = SHA256.Create().ComputeHash(pubBytes)};
        encrypted = Crypto.Encrypt(key, _codec.encode_value(info));
      }
      catch {
        login.cancel();
        Logger.Fatal("Erro na codificação das informações de login.");
        throw;
      }

      int lease;
      LoginInfo l;
      try {
        l = login.login(pubBytes, encrypted, out lease);
      }
      catch (OBJECT_NOT_EXIST) {
        throw new InvalidLoginProcessException();
      }
      catch (WrongEncoding) {
        ServiceFailure e = new ServiceFailure
                           {
                             message =
                               "Erro na codificação da chave pública do barramento."
                           };
        throw e;
      }
      catch (Exception e) {
        Logger.Fatal(e);
        throw;
      }
      LocalLogin(busId, key, l, lease);
    }

    private void LocalLogin(string busId, AsymmetricKeyParameter key,
                            LoginInfo login, int lease) {
      lock (_loginLock) {
        if (_login.IsLoggedIn()) {
          throw new AlreadyLoggedInException();
        }
        BusId = busId;
        _busKey = key;
        _login.SetLoggedIn(login);
        StartLeaseRenewer(lease);
      }
    }

    private void LocalLogout() {
      lock (_loginLock) {
        StopLeaseRenewer();
        if (ReferenceEquals(Manager.GetDispatcher(BusId), this)) {
          Manager.ClearDispatcher(BusId);
        }
        _login.SetLoggedOut();
        EraseBusIdAndKey();
        _outgoingLogin2Session.Clear();
        _profile2Login.Clear();
        _sessionId2Session.Clear();
      }
    }

    private void StartLeaseRenewer(int lease) {
      _leaseRenewer = new LeaseRenewer(this, _acs, lease);
      _leaseRenewer.Start();
      Logger.Debug("Thread de renovação de lease está ativa. Lease = "
                   + _leaseRenewer.Lease + " segundos.");
    }

    private void StopLeaseRenewer() {
      if (_leaseRenewer != null) {
        _leaseRenewer.Finish();
        _leaseRenewer = null;
        Logger.Debug("Thread de renovação de lease desativada.");
      }
    }

    internal void SetLoginsCache(LoginCache cache) {
      _loginsCache = cache;
    }

    internal LoginRegistry LoginRegistry { get; private set; }

    internal IAccessControlService LegacyAccess { get; private set; }

    #endregion

    #region Connection Members

    public ORB ORB { get; private set; }

    public OfferRegistry Offers { get; private set; }

    public string BusId { 
      get {
        lock(_loginLock) {
          return _busId;
        }
      }
      private set {
        lock (_loginLock) {
          _busId = value;
        }
      }
    }

    public LoginInfo? Login {
      get { return _login.Login; }
    }

    public void LoginByPassword(string entity, byte[] password) {
      if (_login.IsLoggedIn()) {
        throw new AlreadyLoggedInException();
      }

      try {
        Manager.IgnoreCurrentThread();

        AsymmetricKeyParameter key;
        string busId = GetBusIdAndKey(out key);

        byte[] encrypted;
        byte[] pubBytes = Crypto.GetPublicKeyInBytes(InternalKey.Public);
        try {
          LoginAuthenticationInfo info =
            new LoginAuthenticationInfo
            {data = password, hash = SHA256.Create().ComputeHash(pubBytes)};
          encrypted = Crypto.Encrypt(key, _codec.encode_value(info));
        }
        catch (WrongEncoding) {
          ServiceFailure e = new ServiceFailure
                             {
                               message =
                                 "Erro na codificação da chave pública do barramento."
                             };
          throw e;
        }
        catch {
          Logger.Fatal("Erro na codificação das informações de login.");
          throw;
        }

        int lease;
        LoginInfo l = _acs.loginByPassword(entity, pubBytes, encrypted,
                                           out lease);
        LocalLogin(busId, key, l, lease);
      }
      finally {
        Manager.UnignoreCurrentThread();
      }
    }

    public void LoginByCertificate(string entity, byte[] privKey) {
      if (_login.IsLoggedIn()) {
        throw new AlreadyLoggedInException();
      }

      try {
        Manager.IgnoreCurrentThread();
        byte[] challenge;
        LoginProcess login = _acs.startLoginByCertificate(entity,
                                                          out
                                                            challenge);
        AsymmetricKeyParameter key = Crypto.CreatePrivateKeyFromBytes(privKey);
        byte[] answer = Crypto.Decrypt(key, challenge);
        LoginByObject(login, answer);
      }
      finally {
        Manager.UnignoreCurrentThread();
      }
    }

    public LoginProcess StartSharedAuth(out byte[] secret) {
      byte[] challenge;
      LoginProcess login = _acs.startLoginBySharedAuth(out challenge);
      secret = Crypto.Decrypt(InternalKey.Private, challenge);
      return login;
    }

    public void LoginBySharedAuth(LoginProcess login, byte[] secret) {
      try {
        Manager.IgnoreCurrentThread();
        LoginByObject(login, secret);
      }
      catch (AccessDenied e) {
        throw new WrongSecretException(e.Message, e);
      }
      finally {
        Manager.UnignoreCurrentThread();
      }
    }

    public bool Logout() {
      lock (_loginLock) {
        if (_login.IsLoggedOut()) {
          return false;
        }
        if (_login.IsInvalid()) {
          LocalLogout();
          return true;
        }
      }

      Connection prev = Manager.Requester;
      try {
        Manager.Requester = this;
        _acs.logout();
      }
      catch (NO_PERMISSION e) {
        if ((e.Minor != InvalidLoginCode.ConstVal) ||
            (e.Status.Equals("COMPLETED_NO"))) {
          throw;
        }
        // já fui deslogado do barramento
        return false;
      }
      finally {
        Manager.Requester = prev;
        LocalLogout();
      }
      return true;
    }

    public InvalidLoginCallback OnInvalidLogin { get; set; }

    public void JoinChain(CallerChain chain) {
      if (chain == null) {
        chain = CallerChain;
      }
      lock (_joinedChainOf) {
        // o remove serve apenas para não lançar exceção na Add.
        _joinedChainOf.Remove(Thread.CurrentThread);
        _joinedChainOf.Add(Thread.CurrentThread, chain);
      }
    }

    public CallerChain CallerChain {
      get {
        Current current = GetPICurrent();
        string piCurrentLoginId;
        try {
          piCurrentLoginId = current.get_slot(_connectionSlotId) as string;
        }
        catch (InvalidSlot e) {
          const string message =
            "Falha inesperada ao acessar o slot da conexão corrente";
          Logger.Fatal(message, e);
          throw new OpenBusException(message);
        }
        string loginId;
        lock (_loginLock) {
          if (!Login.HasValue) {
            return null;
          }
          loginId = Login.Value.id;
        }
        if ((piCurrentLoginId == null) || (!loginId.Equals(piCurrentLoginId))) {
          return null;
        }
        try {
          AnyCredential anyCredential =
            (AnyCredential) current.get_slot(_credentialSlotId);
          if (anyCredential.IsLegacy) {
            Credential credential = anyCredential.LegacyCredential;
            LoginInfo caller = new LoginInfo(credential.identifier,
                                             credential.owner);
            LoginInfo[] originators = credential._delegate.Equals(String.Empty)
                                        ? new LoginInfo[0]
                                        : new[]
                                          {
                                            new LoginInfo("unknown",
                                                          credential._delegate)
                                          };
            return new CallerChainImpl(BusId, caller, originators);
          }
          CallChain chain = UnmarshalCallChain(anyCredential.Credential.chain);
          return new CallerChainImpl(BusId, chain.caller, chain.originators,
                                     anyCredential.Credential.chain);
        }
        catch (InvalidSlot e) {
          const string message =
            "Falha inesperada ao acessar o slot da credencial corrente";
          Logger.Fatal(message, e);
          throw new OpenBusException(message);
        }
      }
    }

    public void ExitChain() {
      _joinedChainOf.Remove(Thread.CurrentThread);
    }

    public CallerChain JoinedChain {
      get {
        CallerChain chain;
        _joinedChainOf.TryGetValue(Thread.CurrentThread, out chain);
        return chain;
      }
    }

    #endregion

    #region Interceptor Methods

    internal void SendRequest(ClientRequestInfo ri) {
      string operation = ri.operation;
      Logger.Debug(
        String.Format(
          "Interceptador cliente iniciando tentativa de chamada à operação {0}.",
          operation));

      LoginInfo login;
      string loginId;
      string loginEntity;
      string busId;
      lock (_loginLock) {
        if (!Login.HasValue) {
          Logger.Debug(
            String.Format(
              "Chamada à operação {0} cancelada devido a não existir login.",
              operation));
          throw new NO_PERMISSION(NoLoginCode.ConstVal,
                                  CompletionStatus.Completed_No);
        }
        login = Login.Value;
        loginId = Login.Value.id;
        loginEntity = Login.Value.entity;
        busId = BusId;
      }

      // armazena login no PICurrent para obter no caso de uma exceção
      Current current = GetPICurrent();
      try {
        current.set_slot(_loginSlotId, new BusLoginInfo(login, busId));
      }
      catch (InvalidSlot e) {
        const string message =
          "Falha inesperada ao acessar o slot da thread corrente";
        Logger.Fatal(message, e);
        throw new OpenBusException(message, e);
      }

      int sessionId = 0;
      int ticket = 0;
      byte[] secret = new byte[0];

      EffectiveProfile profile = new EffectiveProfile(ri.effective_profile);
      string remoteLogin;
      if (!_profile2Login.TryGetValue(profile, out remoteLogin)) {
        remoteLogin = String.Empty;
      }

      ClientSideSession session;
      if (_outgoingLogin2Session.TryGetValue(remoteLogin, out session)) {
        lock (session) {
          sessionId = session.Id;
          ticket = session.Ticket;
          secret = new byte[session.Secret.Length];
          session.Secret.CopyTo(secret, 0);
          Logger.Info(String.Format("Reutilizando sessão {0} com ticket {1}.",
                                    sessionId, ticket));
          session.Ticket++;
        }
      }

      try {
        byte[] hash;
        SignedCallChain chain;
        if (sessionId != 0) {
          hash = CreateCredentialHash(operation, ticket, secret);
          chain = CreateCredentialSignedCallChain(remoteLogin);
          Logger.Info(
            String.Format("Chamada à operação {0} no servidor de login {1}.",
                          operation, remoteLogin));
        }
        else {
          if (Legacy) {
            // Testa se tem cadeia para enviar
            string lastCaller = String.Empty;
            bool isLegacyOnly = false;
            CallerChainImpl callerChain = JoinedChain as CallerChainImpl;
            if (callerChain != null) {
              lastCaller = callerChain.Caller.entity;
              if (callerChain.Signed.signature.Length == 0) {
                // é uma credencial somente 1.5
                isLegacyOnly = true;
              }
            }
            Credential legacyData = new Credential(loginId, loginEntity,
                                                   lastCaller);
            ServiceContext legacyContext =
              new ServiceContext(PrevContextId, _codec.encode_value(legacyData));
            ri.add_request_service_context(legacyContext, false);
            if (isLegacyOnly) {
              // não adiciona credencial 2.0
              return;
            }
          }
          // Cria credencial inválida para iniciar o handshake e obter uma nova sessão
          hash = CreateInvalidCredentialHash();
          chain = CreateInvalidCredentialSignedCallChain();
          Logger.Info(
            String.Format(
              "Inicializando sessão de credencial para requisitar a operação {0} no login {1}.",
              operation, remoteLogin));
        }

        CredentialData data = new CredentialData(BusId, loginId, sessionId,
                                                 ticket, hash, chain);
        ServiceContext serviceContext =
          new ServiceContext(ContextId, _codec.encode_value(data));
        ri.add_request_service_context(serviceContext, false);
      }
      catch (Exception) {
        Logger.Fatal(String.Format("Erro ao tentar enviar a requisição {0}.",
                                   operation));
        throw;
      }
    }

    internal void ReceiveException(ClientRequestInfo ri) {
      String operation = ri.operation;
      Logger.Info(String.Format(
        "A exceção {0} foi interceptada ao tentar realizar a chamada {1}.",
        ri.received_exception_id, operation));

      if (!(ri.received_exception_id.Equals(
        Repository.GetRepositoryID(typeof (NO_PERMISSION))))) {
        return;
      }
      NO_PERMISSION exception = ri.received_exception as NO_PERMISSION;
      if (exception == null) {
        return;
      }

      if (exception.Minor != InvalidCredentialCode.ConstVal) {
        if (exception.Minor == NoCredentialCode.ConstVal) {
          Logger.Warn(String.Format(
            "Servidor remoto alega falta de credencial para a chamada {0}, portanto deve ser um servidor incompatível ou com erro.",
            operation));
          throw new NO_PERMISSION(InvalidRemoteCode.ConstVal,
                                  CompletionStatus.Completed_No);
        }
        if (exception.Minor == InvalidLoginCode.ConstVal) {
          CallOnInvalidLoginCallback(true, ri);
        }
        return;
      }

      CredentialReset requestReset = ReadCredentialReset(ri, exception);
      string remoteLogin = requestReset.login;
      EffectiveProfile profile = new EffectiveProfile(ri.effective_profile);
      _profile2Login.TryAdd(profile, remoteLogin);

      int sessionId = requestReset.session;
      byte[] secret = Crypto.Decrypt(InternalKey.Private,
                                     requestReset.challenge);
      _outgoingLogin2Session.TryAdd(remoteLogin,
                                    new ClientSideSession(sessionId, secret,
                                                          remoteLogin));
      Logger.Info(
        String.Format(
          "Início de sessão de credencial {0} ao tentar requisitar a operação {1} ao login {2}.",
          sessionId, operation, remoteLogin));
      // pede que a chamada original seja relançada
      throw new ForwardRequest(ri.target);
    }

    internal void ReceiveRequest(ServerRequestInfo ri,
                                 AnyCredential anyCredential) {
      string interceptedOperation = ri.operation;
      string loginId;
      AsymmetricKeyParameter busKey;
      lock (_loginLock) {
        if (!Login.HasValue) {
          Logger.Fatal(String.Format("Esta conexão está deslogada."));
          throw new NO_PERMISSION(UnknownBusCode.ConstVal,
                                  CompletionStatus.Completed_No);
        }
        loginId = Login.Value.id;
        busKey = _busKey;
      }
      if (!anyCredential.IsLegacy) {
        if (!anyCredential.Credential.bus.Equals(BusId)) {
          Logger.Fatal(String.Format(
            "A identificação do barramento está errada. O valor recebido foi '{0}' e o esperado era '{1}'.",
            BusId, anyCredential.Credential.bus));
          throw new NO_PERMISSION(UnknownBusCode.ConstVal,
                                  CompletionStatus.Completed_No);
        }
      }

      string login = anyCredential.IsLegacy
                       ? anyCredential.LegacyCredential.identifier
                       : anyCredential.Credential.login;
      // CheckValidity lança exceções
      if (!CheckValidity(anyCredential, ri)) {
        Logger.Warn(String.Format("A credencial {0} está fora da validade.",
                                  login));
        throw new NO_PERMISSION(InvalidLoginCode.ConstVal,
                                CompletionStatus.Completed_No);
      }
      Logger.Info(String.Format("A credencial {0} está na validade.", login));

      if (!anyCredential.IsLegacy) {
        CredentialData credential = anyCredential.Credential;
        ServerSideSession session;
        if (_sessionId2Session.TryGetValue(credential.session, out session)) {
          // CheckTicket já faz o lock no ticket history da sessão
          if (session.CheckTicket(credential.ticket)) {
            byte[] hash = CreateCredentialHash(interceptedOperation,
                                               credential.ticket,
                                               session.Secret);
            IStructuralEquatable eqHash = hash;
            if (eqHash.Equals(credential.hash,
                              StructuralComparisons.StructuralEqualityComparer)) {
              // credencial valida
              // CheckChain pode lançar exceção com InvalidChainCode
              CheckChain(credential.chain, credential.login, loginId, busKey);
              // insere o login no slot para a getCallerChain usar
              try {
                ri.set_slot(_connectionSlotId, loginId);
              }
              catch (InvalidSlot e) {
                const string msg =
                  "Falha ao inserir o identificador de login em seu slot.";
                Logger.Fatal(msg, e);
                throw new OpenBusException(msg, e);
              }
              return;
            }
          }
        }
      }
      else {
        try {
          ri.set_slot(_connectionSlotId, loginId);
        }
        catch (InvalidSlot e) {
          const string msg =
            "Falha ao inserir o identificador de login em seu slot.";
          Logger.Fatal(msg, e);
          throw new OpenBusException(msg, e);
        }
        return;
      }

      // credencial invalida por nao ter sessao conhecida, ticket inválido ou hash errado
      Logger.Fatal("Credencial inválida, enviando CredentialReset.");
      // TODO FIXME
      // Uma explicação detalhada para a linha abaixo encontra-se em um FIXME 
      // no código do interceptador servidor, no método receive_request.
      ri.set_slot(_credentialSlotId, "reset");
    }

    internal void SendException(ServerRequestInfo ri,
                                AnyCredential anyCredential) {
      if (!anyCredential.IsLegacy) {
        // CreateCredentialReset pode lançar exceção com UnverifiedLoginCode e InvalidPublicKeyCode
        byte[] encodedReset =
          CreateCredentialReset(anyCredential.Credential.login);
        ServiceContext replyServiceContext = new ServiceContext(ContextId,
                                                                encodedReset);
        ri.add_reply_service_context(replyServiceContext, false);
      }
    }

    private void CallOnInvalidLoginCallback(bool client, RequestInfo ri) {
      string operation = ri.operation;
      ClientRequestInfo cri = null;
      LoginInfo originalLogin;
      string originalBusId;
      lock (_loginLock) {
        _login.SetInvalid();
        // nesse ponto login necessariamente terá valor, logo nunca será o default
        originalLogin = Login.GetValueOrDefault();
        originalBusId = BusId;
      }
      if (client) {
        // se for interceptação cliente, o login e busId originais estão no PICurrent
        cri = ri as ClientRequestInfo;
        Current current = GetPICurrent();
        try {
          BusLoginInfo info = current.get_slot(_loginSlotId) as BusLoginInfo;
          if (info != null) {
            originalLogin = info.Login;
            originalBusId = info.BusId;
          }
          else {
            Logger.Warn("Falha ao obter o login original para chamar a callback. Utilizando login atual.");
          }
        }
        catch (InvalidSlot e) {
          const string message =
            "Falha inesperada ao acessar o slot da conexão corrente";
          Logger.Fatal(message, e);
          throw new OpenBusException(message);
        }
      }
      if (OnInvalidLogin != null) {
        OnInvalidLogin.InvalidLogin(this, originalLogin, originalBusId);
      }
      LoginInfo newLogin;
      string newBusId;
      if (VerifyStatusAfterCallback(operation, cri, originalLogin.id, out newLogin, out newBusId)) {
        // estamos no interceptador servidor e o relogin funcionou, então retorna
        return;
      }
      // estamos no interceptador cliente e o login está inválido mas mudou, tenta callback de novo
      while (true) {
        if (OnInvalidLogin != null) {
          OnInvalidLogin.InvalidLogin(this, newLogin, newBusId);
        }
        VerifyStatusAfterCallback(operation, cri, newLogin.id, out newLogin, out newBusId);
        // se retornou novamente, tenta de novo indefinidamente (opção discutida por email com subject OPENBUS-1819 em 10/07/12)
      }
    }

    private bool VerifyStatusAfterCallback(string operation, ClientRequestInfo ri, string originalLogin, out LoginInfo newLogin, out string newBusId) {
      lock (_loginLock) {
        if (_login.IsLoggedIn()) {
          Logger.Debug("Login reestabelecido.");
          if (ri != null) {
            Logger.Debug(String.Format(
              "Solicitando que a chamada {0} seja refeita.",
              operation));
            throw new ForwardRequest(ri.target);
          }
          newLogin = new LoginInfo();
          newBusId = null;
          return true;
        }
        if (_login.IsLoggedOut()) {
          LogoutAfterUnsuccessfulCallback(operation, ri);
        }
        // login está inválido mesmo depois da callback
        if (Login.HasValue) {
          if (Login.Value.id.Equals(originalLogin)) {
            LogoutAfterUnsuccessfulCallback(operation, ri);
          }
          // login mudou, copia o novo login para uma variável local
          newLogin = new LoginInfo(Login.Value.id, Login.Value.entity);
          newBusId = BusId;
        }
        else {
          // esse caso nunca ocorre, está aqui apenas para evitar warning.
          newLogin = new LoginInfo();
          newBusId = "";
        }
        return false;
      }
    }

    private void LogoutAfterUnsuccessfulCallback(string operation, ClientRequestInfo ri) {
      LocalLogout();
      string s = "receber";
      if (ri != null) {
        s = "realizar";
      }
      Logger.Fatal(String.Format(
        "Login não foi reestabelecido, impossível {0} a chamada {1}.", s,
        operation));
      // no caso do servidor, o ServerInterceptor.cs transformará essa exceção em UnverifiedLoginCode
      throw new NO_PERMISSION(NoLoginCode.ConstVal, CompletionStatus.Completed_No);
    }

    private byte[] CreateInvalidCredentialHash() {
      return new byte[HashValueSize.ConstVal];
    }

    private SignedCallChain CreateCredentialSignedCallChain(string remoteLogin) {
      SignedCallChain signed;
      CallerChainImpl chain = JoinedChain as CallerChainImpl;
      if (!remoteLogin.Equals(BusLogin.ConstVal)) {
        // esta requisição não é para o barramento, então preciso assinar essa cadeia.
        if (chain == null) {
          // na chamada a signChainFor vai criar uma nova chain e assinar
          signed = _acs.signChainFor(remoteLogin);
        }
        else {
          lock (chain) {
            if (!chain.Joined.TryGetValue(remoteLogin, out signed)) {
              signed = _acs.signChainFor(remoteLogin);
              chain.Joined.TryAdd(remoteLogin, signed);
            }
          }
        }
      }
      else {
        // requisição para o barramento
        if (chain == null) {
          return CreateInvalidCredentialSignedCallChain();
        }
        signed = chain.Signed;
      }
      return signed;
    }

    private SignedCallChain CreateInvalidCredentialSignedCallChain() {
      return new SignedCallChain(new byte[EncryptedBlockSize.ConstVal],
                                 new byte[0]);
    }

    private CredentialReset ReadCredentialReset(ClientRequestInfo ri,
                                                NO_PERMISSION exception) {
      CredentialReset requestReset;

      try {
        ServiceContext serviceContext =
          ri.get_reply_service_context(ContextId);

        Type resetType = typeof (CredentialReset);
        TypeCode resetTypeCode = ORB.create_interface_tc(
          Repository.GetRepositoryID(resetType), resetType.Name);

        byte[] data = serviceContext.context_data;
        requestReset =
          (CredentialReset) _codec.decode_value(data, resetTypeCode);
      }
      catch (Exception e) {
        Logger.Fatal(
          "Erro na tentativa de extrair a informação de reset.", e);
        throw new NO_PERMISSION(InvalidRemoteCode.ConstVal, exception.Status);
      }
      return requestReset;
    }

    private byte[] CreateCredentialReset(string remoteLogin) {
      string loginId;
      lock (_loginLock) {
        if (!Login.HasValue) {
          // Este servidor não está logado no barramento
          Logger.Fatal(
            "Este servidor não está logado no barramento e portanto não pode criar um CredentialReset.");
          throw new NO_PERMISSION(UnknownBusCode.ConstVal,
                                  CompletionStatus.Completed_No);
        }
        loginId = Login.Value.id;
      }
      CredentialReset reset = new CredentialReset {login = loginId};
      byte[] challenge = new byte[SecretSize];
      Random rand = new Random();
      rand.NextBytes(challenge);

      AsymmetricKeyParameter pubKey;
      string entity;
      if (!_loginsCache.GetLoginEntity(remoteLogin, this,
                                       out entity,
                                       out pubKey)) {
        Logger.Warn(
          "Não foi encontrada uma entrada na cache de logins para o login " +
          remoteLogin);
        throw new NO_PERMISSION(UnverifiedLoginCode.ConstVal,
                                CompletionStatus.Completed_No);
      }
      try {
        reset.challenge = Crypto.Encrypt(pubKey, challenge);
      }
      catch (Exception) {
        throw new NO_PERMISSION(InvalidPublicKeyCode.ConstVal,
                                CompletionStatus.Completed_No);
      }

      // lock para tornar esse trecho atomico
      int sessionId;
      lock (_sessionIdLock) {
        sessionId = _sessionId;
        _sessionId++;
      }
      ServerSideSession session = new ServerSideSession(sessionId,
                                                        challenge,
                                                        remoteLogin);
      lock (session) {
        _sessionId2Session.TryAdd(session.Id, session);
        reset.session = session.Id;
      }
      return _codec.encode_value(reset);
    }

    private bool CheckValidity(AnyCredential anyCredential, RequestInfo ri) {
      while (true) {
        try {
          return _loginsCache.ValidateLogin(anyCredential, this);
        }
        catch (Exception e) {
          NO_PERMISSION noPermission = e as NO_PERMISSION;
          if ((noPermission != null) &&
              (noPermission.Minor == InvalidLoginCode.ConstVal)) {
            Logger.Fatal(
              "Este servidor foi deslogado do barramento durante a interceptação desta requisição.");
            // chama callback e tenta de novo
            CallOnInvalidLoginCallback(false, ri);
          }
          Logger.Fatal(
            "Não foi possível validar a credencial. Erro: " + e);
          throw new NO_PERMISSION(UnverifiedLoginCode.ConstVal,
                                  CompletionStatus.Completed_No);
        }
      }
    }

    private CallChain UnmarshalCallChain(SignedCallChain signed) {
      Type chainType = typeof (CallChain);
      TypeCode chainTypeCode =
        ORB.create_interface_tc(Repository.GetRepositoryID(chainType),
                                chainType.Name);
      return (CallChain) _codec.decode_value(signed.encoded, chainTypeCode);
    }

    private void CheckChain(SignedCallChain signed, string callerId,
                            string loginId, AsymmetricKeyParameter busKey) {
      CallChain chain = UnmarshalCallChain(signed);
      if (!chain.target.Equals(loginId)) {
        Logger.Fatal(
          "O login não é o mesmo do alvo da cadeia. É necessário refazer a sessão de credencial através de um reset.");
        throw new NO_PERMISSION(InvalidCredentialCode.ConstVal,
                                CompletionStatus.Completed_No);
      }
      if (!chain.caller.id.Equals(callerId) ||
          (!Crypto.VerifySignature(busKey, signed.encoded, signed.signature))) {
        Logger.Fatal("Cadeia de credencial inválida.");
        throw new NO_PERMISSION(InvalidChainCode.ConstVal,
                                CompletionStatus.Completed_No);
      }
    }

    private byte[] CreateCredentialHash(string operation, int ticket,
                                        byte[] secret) {
      System.Text.Encoding enc = Crypto.TextEncoding;
      // 2 bytes para versao, 16 para o segredo, 4 para o ticket em little endian e X para a operacao.
      int size = 2 + secret.Length + 4 + enc.GetByteCount(operation);
      byte[] hash = new byte[size];
      hash[0] = MajorVersion;
      hash[1] = MinorVersion;
      int index = 2;
      secret.CopyTo(hash, index);
      index += secret.Length;
      byte[] bTicket = BitConverter.GetBytes(ticket);
      if (!BitConverter.IsLittleEndian) {
        Array.Reverse(bTicket);
      }
      bTicket.CopyTo(hash, index);
      index += 4;
      byte[] bOperation = enc.GetBytes(operation);
      bOperation.CopyTo(hash, index);
      return SHA256.Create().ComputeHash(hash);
    }

    private sealed class LoginStatus {
      private enum LStatus {
        LoggedIn,
        Invalid,
        LoggedOut
      }

      private LStatus _status;
      private LoginInfo? _login;
      private readonly ConnectionImpl _conn;

      internal LoginStatus(ConnectionImpl conn) {
        _conn = conn;
        SetLoggedOut();
      }

      private LStatus Status {
        get {
          // Não há problema em deixar o return dentro do lock pois o compilador
          // é esperto o suficiente para lidar com isso corretamente.
          lock (_conn._loginLock) {
            return _status;
          }
        }
      }

      internal LoginInfo? Login {
        get {
          lock (_conn._loginLock) {
            return _login;
          }
        }
      }

      internal void SetLoggedIn(LoginInfo login) {
        lock (_conn._loginLock) {
          _login = login;
          _status = LStatus.LoggedIn;
        }
      }

      internal void SetLoggedOut() {
        lock (_conn._loginLock) {
          _login = null;
          _status = LStatus.LoggedOut;
        }
      }

      internal void SetInvalid() {
        lock (_conn._loginLock) {
          _status = LStatus.Invalid;
        }
      }

      internal bool IsLoggedIn() {
        return Status == LStatus.LoggedIn;
      }

      internal bool IsLoggedOut() {
        return Status == LStatus.LoggedOut;
      }

      internal bool IsInvalid() {
        return Status == LStatus.Invalid;
      }
    }

    private sealed class BusLoginInfo {
      internal readonly LoginInfo Login;
      internal readonly string BusId;

      internal BusLoginInfo(LoginInfo login, string busId) {
        Login = login;
        BusId = busId;
      }
    }

    #endregion
  }
}