using System;
using System.Collections;
using System.Collections.Concurrent;
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
using tecgraf.openbus.core.v2_00;
using tecgraf.openbus.core.v2_00.credential;
using tecgraf.openbus.core.v2_00.services.access_control;
using tecgraf.openbus.core.v2_00.services.offer_registry;
using tecgraf.openbus.sdk.Standard.Interceptors;
using tecgraf.openbus.sdk.exceptions;
using tecgraf.openbus.sdk.Security;
using tecgraf.openbus.sdk.interceptors;
using tecgraf.openbus.sdk.lease;
using TypeCode = omg.org.CORBA.TypeCode;

namespace tecgraf.openbus.sdk.Standard {
  internal class StandardConnection : Connection {
    #region Fields

    private static readonly ILog Logger =
      LogManager.GetLogger(typeof (StandardConnection));

    private readonly string _host;
    private readonly short _port;
    private readonly bool _legacy;
    private readonly IComponent _acsComponent;
    private AccessControl _acs;
    private LoginRegistry _lr;
    private AsymmetricKeyParameter _busKey;
    private LeaseRenewer _leaseRenewer;

    private volatile int _sessionId;
    private readonly object _lock = new object();
    //TODO: caches ainda nao tem nenhuma politica de remocao ou de tamanho maximo
    // client caches
    private readonly ConcurrentDictionary<EffectiveProfile, string>
      _profile2Login;

    private readonly ConcurrentDictionary<string, ClientSideSession>
      _outgoingLogin2Session;

    private readonly ConditionalWeakTable<Thread, CallerChain> _joinedChainOf;

    // server caches
    private readonly ConcurrentDictionary<int, ServerSideSession>
      _sessionId2Session;

    private readonly ConcurrentDictionary<string, AsymmetricKeyParameter>
      _login2PubKey;

    private readonly ConditionalWeakTable<Thread, CallerChain>
      _callerChainOf;

    /// <summary>
    /// Representam a identificação dos "service contexts" (contextos) utilizados
    /// para transporte de credenciais em requisições de serviço.
    /// </summary>
    private const int ContextId = CredentialContextId.ConstVal;

    private const int PrevContextId = 1234;

    private const byte MajorVersion = core.v2_00.MajorVersion.ConstVal;
    private const byte MinorVersion = core.v2_00.MinorVersion.ConstVal;
    //TODO: revisar se precisa desse SecretSize
    private const int SecretSize = 16;

    private readonly Codec _codec;

    #endregion

    #region Constructors

    internal StandardConnection(string host, short port) {
      if (string.IsNullOrEmpty(host)) {
        throw new ArgumentException("O campo 'host' não é válido");
      }
      if (port < 0) {
        throw new ArgumentException("O campo 'port' não pode ser negativo.");
      }
      _host = host;
      _port = port;
      //TODO: transformar o valor de _legacy num parâmetro futuramente, caso seja decidido dar suporte a ligar/desligar a comunicação com clientes legados na API.
      _legacy = true;
      _codec = StandardServerInterceptor.Instance.Codec;
      _acsComponent = RemotingServices.Connect(
        typeof (IComponent),
        "corbaloc::1.0@" + _host + ":" + _port + "/" + BusObjectKey.ConstVal)
                      as IComponent;

      InternalKey = Crypto.GenerateKeyPair();

      _sessionId2Session = new ConcurrentDictionary<int, ServerSideSession>();
      _login2PubKey =
        new ConcurrentDictionary<string, AsymmetricKeyParameter>();
      _profile2Login = new ConcurrentDictionary<EffectiveProfile, string>();
      _outgoingLogin2Session =
        new ConcurrentDictionary<String, ClientSideSession>();
      _callerChainOf = new ConditionalWeakTable<Thread, CallerChain>();
      _joinedChainOf = new ConditionalWeakTable<Thread, CallerChain>();

      StandardServerInterceptor.Instance.Connection = this;
      StandardClientInterceptor.Instance.Connection = this;

      IgnoreLogin = true;
      GetBusFacets();
      IgnoreLogin = false;
    }

    #endregion

    #region Internal Members

    private void GetBusFacets() {
      if (IsLoggedIn()) {
        return;
      }
      String acsId = Repository.GetRepositoryID(typeof (AccessControl));
      String lrId = Repository.GetRepositoryID(typeof (LoginRegistry));
      String orId = Repository.GetRepositoryID(typeof (OfferRegistry));

      MarshalByRefObject acsObjRef = _acsComponent.getFacet(acsId);
      MarshalByRefObject lrObjRef = _acsComponent.getFacet(lrId);
      MarshalByRefObject orObjRef = _acsComponent.getFacet(orId);

      _acs = acsObjRef as AccessControl;
      _lr = lrObjRef as LoginRegistry;
      OfferRegistry = orObjRef as OfferRegistry;
      if ((_acs == null) || (_lr == null) || (OfferRegistry == null)) {
        Logger.Error("O serviço de controle de acesso não foi encontrado");
        return;
      }

      BusId = _acs.busid;
      _busKey = Crypto.CreatePublicKeyFromBytes(_acs.buskey);
    }

    private AsymmetricCipherKeyPair InternalKey { get; set; }

    private bool IsLoggedIn() {
      return (Login != null);
    }

    private void LoginByObject(LoginProcess login, byte[] secret) {
      if (IsLoggedIn()) {
        login.cancel();
        throw new AlreadyLoggedInException();
      }

      byte[] encrypted;
      byte[] pubBytes = Crypto.GetPublicKeyInBytes(InternalKey.Public);
      try {
        //encode answer and hash of public key
        LoginAuthenticationInfo info =
          new LoginAuthenticationInfo
          {data = secret, hash = SHA256.Create().ComputeHash(pubBytes)};
        encrypted = Crypto.Encrypt(_busKey, _codec.encode_value(info));
      }
      catch {
        login.cancel();
        Logger.Fatal("Erro na codificação das informações de login.");
        throw;
      }

      int lease;
      try {
        Login = login.login(pubBytes, encrypted, out lease);
      }
      catch (Exception e) {
        Logger.Fatal(e.Message);
        Logger.Fatal(e.StackTrace);
        throw;
      }
      StartLeaseRenewer(lease);
    }

    private void LocalLogout() {
      Login = null;
      StopLeaseRenewer();
      //TODO: resetar caches restantes se e qdo forem implementados
      _login2PubKey.Clear();
      _outgoingLogin2Session.Clear();
      _profile2Login.Clear();
      _sessionId2Session.Clear();
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

    private bool IgnoreLogin { get; set; }

    #endregion

    #region Connection Members

    public OfferRegistry OfferRegistry { get; private set; }

    public string BusId { get; private set; }

    //TODO: avaliar se tem que tornar thread-safe
    public LoginInfo? Login { get; private set; }

    public void LoginByPassword(string entity, byte[] password) {
      if (IsLoggedIn()) {
        throw new AlreadyLoggedInException();
      }

      try {
        IgnoreLogin = true;
        byte[] encrypted;
        byte[] pubBytes = Crypto.GetPublicKeyInBytes(InternalKey.Public);
        try {
          LoginAuthenticationInfo info =
            new LoginAuthenticationInfo
            {data = password, hash = SHA256.Create().ComputeHash(pubBytes)};
          encrypted = Crypto.Encrypt(_busKey, _codec.encode_value(info));
        }
        catch {
          Logger.Fatal("Erro na codificação das informações de login.");
          throw;
        }

        int lease;
        Login = _acs.loginByPassword(entity, pubBytes, encrypted, out lease);
        StartLeaseRenewer(lease);
      }
      finally {
        IgnoreLogin = false;
      }
    }

    public void LoginByCertificate(string entity, byte[] privKey) {
      if (IsLoggedIn()) {
        throw new AlreadyLoggedInException();
      }

      try {
        IgnoreLogin = true;
        byte[] challenge;
        LoginProcess login = _acs.startLoginByCertificate(entity,
                                                          out
                                                            challenge);
        AsymmetricKeyParameter key = Crypto.CreatePrivateKeyFromBytes(privKey);
        byte[] answer = Crypto.Decrypt(key, challenge);
        LoginByObject(login, answer);
      }
      finally {
        IgnoreLogin = false;
      }
    }

    public LoginProcess StartSingleSignOn(out byte[] secret) {
      byte[] challenge;
      LoginProcess login = _acs.startLoginBySingleSignOn(out challenge);
      secret = Crypto.Decrypt(InternalKey.Private, challenge);
      return login;
    }

    public void LoginBySingleSignOn(LoginProcess login, byte[] secret) {
      try {
        IgnoreLogin = true;
        LoginByObject(login, secret);
      }
      finally {
        IgnoreLogin = false;
      }
    }

    public bool Logout() {
      if (Login == null) {
        return false;
      }

      try {
        _acs.logout();
      }
      catch (NO_PERMISSION e) {
        if ((e.Minor != InvalidLoginCode.ConstVal) ||
            (e.Status.Equals("COMPLETED_NO"))) {
          throw;
        }
        // já fui deslogado do barramento
        LocalLogout();
        return false;
      }
      LocalLogout();
      return true;
    }

    public InvalidLoginCallback OnInvalidLoginCallback { get; set; }

    public void JoinChain(CallerChain chain) {
      if (chain == null) {
        _callerChainOf.TryGetValue(Thread.CurrentThread, out chain);
      }
      lock (_joinedChainOf) {
        // o remove serve apenas para não lançar exceção na Add.
        _joinedChainOf.Remove(Thread.CurrentThread);
        _joinedChainOf.Add(Thread.CurrentThread, chain);
      }
    }

    public CallerChain CallerChain {
      get {
        CallerChain chain;
        _callerChainOf.TryGetValue(Thread.CurrentThread, out chain);
        return chain;
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

    public void Close() {
      try {
        Logout();
      }
      catch (Exception e) {
        Logger.Warn(String.Format("Erro capturado ao fechar conexão: {0}",
                                  e.Message));
      }
      finally {
        StandardServerInterceptor.Instance.Connection = null;
        StandardClientInterceptor.Instance.Connection = null;
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

      string loginId;
      string loginEntity;
      if (!IgnoreLogin) {
        if (!Login.HasValue) {
          Logger.Debug(
            String.Format(
              "Chamada à operação {0} cancelada devido a não existir login.",
              operation));
          throw new NO_PERMISSION(NoLoginCode.ConstVal,
                                  CompletionStatus.Completed_No);
        }
        loginId = Login.Value.id;
        loginEntity = Login.Value.entity;
      }
      else {
        Logger.Info("O login está sendo ignorado para esta chamada.");
        return;
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
          if (_legacy) {
            // Testa se tem cadeia para enviar
            string lastCaller = String.Empty;
            CallerChain callerChain = JoinedChain;
            if (callerChain != null) {
              if (callerChain.Callers.Length > 1) {
                lastCaller = callerChain.Callers[0].entity;
              }
            }
            Credential legacyData = new Credential(loginId, loginEntity,
                                                   lastCaller);
            ServiceContext legacyContext =
              new ServiceContext(PrevContextId, _codec.encode_value(legacyData));
            ri.add_request_service_context(legacyContext, false);
            if (lastCaller.Equals(String.Empty)) {
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
      catch (NO_PERMISSION e) {
        if (e.Minor == InvalidLoginCode.ConstVal) {
          Logger.Fatal(
            "Este cliente foi deslogado do barramento durante a interceptação desta requisição.",
            e);
          LocalLogout();
          if ((OnInvalidLoginCallback != null) &&
              (OnInvalidLoginCallback.InvalidLogin(this))) {
            // pede que a chamada original seja relançada
            throw new ForwardRequest(ri.target);
          }
          // se callback retornar false, tentativas de refazer login falharam, lança exceção
          throw new NO_PERMISSION(InvalidLoginCode.ConstVal,
                                  CompletionStatus.Completed_No);
        }
        throw;
      }
      catch (Exception) {
        Logger.Fatal(String.Format("Erro ao tentar enviar a requisição {0}.",
                                   operation));
        throw;
      }
    }

    internal void SendReply(ServerRequestInfo ri) {
      _callerChainOf.Remove(Thread.CurrentThread);
    }

    internal void ReceiveException(ClientRequestInfo ri) {
      String operation = ri.operation;
      Logger.Info(String.Format(
        "A exceção {0} foi interceptada ao tentar realizar a chamada {1}.",
        ri.received_exception_id, operation));

      if (
        !(ri.received_exception_id.Equals(
          Repository.GetRepositoryID(typeof (NO_PERMISSION))))) {
        return;
      }
      NO_PERMISSION exception = ri.received_exception as NO_PERMISSION;
      if (exception == null) {
        return;
      }

      if (exception.Minor != InvalidCredentialCode.ConstVal) {
        if (exception.Minor == InvalidLoginCode.ConstVal) {
          LocalLogout();
          if ((OnInvalidLoginCallback != null) &&
              (OnInvalidLoginCallback.InvalidLogin(this))) {
            Logger.Info(String.Format(
              "Login reestabelecido, solicitando que a chamada {0} seja refeita.",
              operation));
            throw new ForwardRequest(ri.target);
          }
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

    internal void ReceiveRequest(ServerRequestInfo ri) {
      String interceptedOperation = ri.operation;
      Logger.Info(String.Format(
        "A operação '{0}' foi interceptada no servidor.", interceptedOperation));

      bool legacyContext;
      ServiceContext serviceContext =
        GetContextFromRequestInfo(ri, out legacyContext);
      AnyCredential anyCredential = UnmarshalCredential(serviceContext,
                                                        legacyContext);
      Logger.Info(
        String.Format("A operação '{0}' possui credencial. É legada? {1}.",
                      interceptedOperation, legacyContext));
      if (!anyCredential.IsLegacy) {
        // aqui só tenho uma conexão com um barramento, então a chamada CheckValidity abaixo já garante que a credencial legacy recebida é do mesmo barramento (não precisa entrar nesse bloco).
        if (!anyCredential.Credential.bus.Equals(BusId)) {
          Logger.Fatal(String.Format(
            "A identificação do barramento está errada. O valor recebido foi '{0}' e o esperado era '{1}'.",
            BusId, anyCredential.Credential.bus));
          throw new NO_PERMISSION(UnknownBusCode.ConstVal,
                                  CompletionStatus.Completed_No);
        }
      }

      // CheckValidity lança exceção caso a validade tenha expirado
      CheckValidity(anyCredential);

      if (!anyCredential.IsLegacy) {
        CredentialData credential = anyCredential.Credential;
        ServerSideSession session;
        if (_sessionId2Session.TryGetValue(credential.session, out session)) {
          // CheckTicket já faz o lock no ticket history da sessão
          if (session.CheckTicket(credential.ticket)) {
            byte[] hash = CreateCredentialHash(ri.operation, credential.ticket,
                                               session.Secret);
            IStructuralEquatable eqHash = hash;
            if (eqHash.Equals(credential.hash,
                              StructuralComparisons.StructuralEqualityComparer)) {
              // credencial valida
              // CheckChain pode lançar exceção com InvalidChainCode ou UnverifiedLoginCode
              CheckChain(credential.chain, credential.login);
              return;
            }
          }
        }
      }
      else {
        Credential credential = anyCredential.LegacyCredential;
        LoginInfo[] callers;
        if (credential._delegate.Equals(String.Empty)) {
          callers = new[]
                    {new LoginInfo(credential.identifier, credential.owner)};
        }
        else {
          LoginInfo caller = new LoginInfo("unknown", credential._delegate);
          callers = new[] {caller, caller};
        }
        CallerChainImpl callerChain = new CallerChainImpl(BusId, callers);
        _callerChainOf.Add(Thread.CurrentThread, callerChain);
      }

      // credencial invalida por nao ter sessao conhecida, ticket inválido ou hash errado
      Logger.Fatal("Credencial inválida, enviando CredentialReset.");
      throw new NO_PERMISSION(InvalidCredentialCode.ConstVal,
                              CompletionStatus.Completed_No);
    }

    internal void SendException(ServerRequestInfo ri) {
      String interceptedOperation = ri.operation;
      Logger.Info(String.Format(
        "O lançamento de uma exceção para a operação '{0}' foi interceptado no servidor.",
        interceptedOperation));

      NO_PERMISSION ex = ri.sending_exception as NO_PERMISSION;
      if (ex == null) {
        return;
      }
      if (ex.Minor == InvalidCredentialCode.ConstVal) {
        bool legacyContext;
        ServiceContext serviceContext =
          GetContextFromRequestInfo(ri, out legacyContext);
        // credencial é inválida
        AnyCredential credential = UnmarshalCredential(serviceContext,
                                                       legacyContext);
        Logger.Info(String.Format(
          "A operação '{0}' para a qual será lançada a exceção possui credencial. Legada? {1}.",
          interceptedOperation, credential.IsLegacy));
        if (!credential.IsLegacy) {
          // CreateCredentialReset pode lançar exceção com UnverifiedLoginCode
          byte[] encodedReset =
            CreateCredentialReset(credential.Credential.login);
          ServiceContext replyServiceContext = new ServiceContext(ContextId,
                                                                  encodedReset);
          ri.add_reply_service_context(replyServiceContext, false);
        }
      }
    }

    private ServiceContext GetContextFromRequestInfo(RequestInfo ri,
                                                     out bool legacyContext) {
      legacyContext = false;
      String interceptedOperation = ri.operation;
      ServiceContext serviceContext;
      try {
        serviceContext = ri.get_request_service_context(ContextId);
      }
      catch (BAD_PARAM) {
        if (_legacy) {
          return GetLegacyContextFromRequestInfo(ri, out legacyContext);
        }
        Logger.Warn(String.Format(
          "A chamada à operação '{0}' não possui credencial.",
          interceptedOperation));
        throw new NO_PERMISSION(InvalidLoginCode.ConstVal,
                                CompletionStatus.Completed_No);
      }
      return serviceContext;
    }

    private ServiceContext GetLegacyContextFromRequestInfo(RequestInfo ri,
                                                           out bool
                                                             legacyContext) {
      String interceptedOperation = ri.operation;
      ServiceContext serviceContext;
      try {
        serviceContext = ri.get_request_service_context(PrevContextId);
      }
      catch (BAD_PARAM) {
        Logger.Warn(String.Format(
          "A chamada à operação '{0}' não possui credencial.",
          interceptedOperation));
        throw new NO_PERMISSION(InvalidLoginCode.ConstVal,
                                CompletionStatus.Completed_No);
      }
      legacyContext = true;
      return serviceContext;
    }

    private byte[] CreateInvalidCredentialHash() {
      return new byte[HashValueSize.ConstVal];
    }

    private SignedCallChain CreateCredentialSignedCallChain(string remoteLogin) {
      SignedCallChain signed;
      CallerChainImpl chain = JoinedChain as CallerChainImpl;
      if ((chain == null) &&
          ((remoteLogin.Equals(BusId)) || (remoteLogin.Equals(String.Empty)))) {
        return CreateInvalidCredentialSignedCallChain();
      }
      if (chain == null) {
        // na chamada a signChainFor vai criar uma nova chain e assinar
        signed = _acs.signChainFor(remoteLogin);
      }
      else {
        lock (chain) {
          if (!remoteLogin.Equals(BusId)) {
            if (!chain.Joined.TryGetValue(remoteLogin, out signed)) {
              signed = _acs.signChainFor(remoteLogin);
              chain.Joined.TryAdd(remoteLogin, signed);
            }
          }
          else {
            signed = chain.Signed;
          }
        }
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

        OrbServices orb = OrbServices.GetSingleton();
        Type resetType = typeof (CredentialReset);
        TypeCode resetTypeCode = orb.create_interface_tc(
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

    private AnyCredential UnmarshalCredential(ServiceContext serviceContext,
                                              bool legacyContext) {
      OrbServices orb = OrbServices.GetSingleton();
      if (legacyContext) {
        return UnmarshalLegacyCredential(serviceContext);
      }
      Type credentialType = typeof (CredentialData);
      TypeCode credentialTypeCode =
        orb.create_interface_tc(Repository.GetRepositoryID(credentialType),
                                credentialType.Name);

      byte[] data = serviceContext.context_data;
      return new AnyCredential(
        (CredentialData) _codec.decode_value(data, credentialTypeCode));
    }

    private AnyCredential UnmarshalLegacyCredential(
      ServiceContext serviceContext) {
      // will use bus value for delegate
      OrbServices orb = OrbServices.GetSingleton();
      Type credentialType = typeof (Credential);
      TypeCode credentialTypeCode =
        orb.create_interface_tc(Repository.GetRepositoryID(credentialType),
                                credentialType.Name);

      byte[] data = serviceContext.context_data;
      Credential cred =
        (Credential) _codec.decode_value(data, credentialTypeCode);
      return new AnyCredential(cred);
    }

    private byte[] CreateCredentialReset(string remoteLogin) {
      if (Login != null) {
        CredentialReset reset = new CredentialReset {login = Login.Value.id};
        byte[] challenge = new byte[SecretSize];
        Random rand = new Random();
        rand.NextBytes(challenge);

        // lock para tornar esse trecho atomico
        lock (_lock) {
          // lock necessário para garantir atomicidade entre o get e o add
          AsymmetricKeyParameter pubKey;
          lock (_login2PubKey) {
            if (!_login2PubKey.TryGetValue(remoteLogin, out pubKey)) {
              byte[] key;
              try {
                _lr.getLoginInfo(remoteLogin, out key);
              }
              catch (NO_PERMISSION e) {
                if (e.Minor == InvalidLoginCode.ConstVal) {
                  Logger.Fatal(
                    "Este servidor foi deslogado do barramento durante a interceptação desta requisição",
                    e);
                  throw new NO_PERMISSION(UnverifiedLoginCode.ConstVal,
                                          CompletionStatus.Completed_No);
                }
                throw;
              }
              pubKey = Crypto.CreatePublicKeyFromBytes(key);
              _login2PubKey.TryAdd(remoteLogin, pubKey);
            }
          }

          try {
            reset.challenge = Crypto.Encrypt(pubKey, challenge);
          }
          catch (Exception) {
            throw new NO_PERMISSION(InvalidPublicKeyCode.ConstVal,
                                    CompletionStatus.Completed_No);
          }

          ServerSideSession session = new ServerSideSession(_sessionId,
                                                            challenge,
                                                            remoteLogin);
          lock (session) {
            _sessionId2Session.TryAdd(session.Id, session);
            reset.session = session.Id;
          }
          _sessionId++;
        }
        return _codec.encode_value(reset);
      }
      // Este servidor não está logado no barramento
      Logger.Fatal("Este servidor não está logado no barramento");
      throw new NO_PERMISSION(UnverifiedLoginCode.ConstVal,
                              CompletionStatus.Completed_No);
    }

    private void CheckValidity(AnyCredential anyCredential) {
      int validity;
      string login = anyCredential.IsLegacy
                       ? anyCredential.LegacyCredential.identifier
                       : anyCredential.Credential.login;
      try {
        validity = _lr.getValidity(new[] {login})[0];
      }
      catch (NO_PERMISSION e) {
        if (e.Minor == InvalidLoginCode.ConstVal) {
          Logger.Fatal(
            "Este servidor foi deslogado do barramento durante a interceptação desta requisição",
            e);
          throw new NO_PERMISSION(UnverifiedLoginCode.ConstVal,
                                  CompletionStatus.Completed_No);
        }
        throw;
      }
      if (validity <= 0) {
        Logger.Warn(String.Format("A credencial {0} está fora da validade.",
                                  login));
        throw new NO_PERMISSION(InvalidLoginCode.ConstVal,
                                CompletionStatus.Completed_No);
      }
      Logger.Info(String.Format("A credencial {0} está na validade.", login));
    }

    private CallChain UnmarshalCallChain(SignedCallChain signed) {
      OrbServices orb = OrbServices.GetSingleton();
      Type chainType = typeof (CallChain);
      TypeCode chainTypeCode =
        orb.create_interface_tc(Repository.GetRepositoryID(chainType),
                                chainType.Name);
      return (CallChain) _codec.decode_value(signed.encoded, chainTypeCode);
    }

    private void CheckChain(SignedCallChain signed, string callerId) {
      CallChain chain = UnmarshalCallChain(signed);
      try {
        if (!chain.target.Equals(Login.Value.id) ||
            (!chain.callers[chain.callers.Length - 1].id.Equals(callerId)) ||
            (!Crypto.VerifySignature(_busKey, signed.encoded, signed.signature))) {
          Logger.Fatal("Cadeia de credencial inválida.");
          throw new NO_PERMISSION(InvalidChainCode.ConstVal,
                                  CompletionStatus.Completed_No);
        }
        CallerChainImpl callerChain = new CallerChainImpl(BusId, chain.callers,
                                                          signed);
        _callerChainOf.Add(Thread.CurrentThread, callerChain);
      }
      catch (InvalidOperationException) {
        Logger.Fatal(
          "Este servidor foi deslogado do barramento durante a interceptação desta requisição");
        throw new NO_PERMISSION(UnverifiedLoginCode.ConstVal,
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

    #endregion

    private class AnyCredential {
      public CredentialData Credential;
      public Credential LegacyCredential;
      public readonly bool IsLegacy;

      public AnyCredential(CredentialData credential) {
        IsLegacy = false;
        Credential = credential;
      }

      public AnyCredential(Credential credential) {
        IsLegacy = true;
        LegacyCredential = credential;
      }
    }
  }
}