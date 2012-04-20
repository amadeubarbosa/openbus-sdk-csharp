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
using tecgraf.openbus.core.v2_00;
using tecgraf.openbus.core.v2_00.credential;
using tecgraf.openbus.core.v2_00.services.access_control;
using tecgraf.openbus.core.v2_00.services.offer_registry;
using tecgraf.openbus.sdk.caches;
using tecgraf.openbus.sdk.exceptions;
using tecgraf.openbus.sdk.interceptors;
using tecgraf.openbus.sdk.lease;
using tecgraf.openbus.sdk.security;
using Current = omg.org.PortableInterceptor.Current;
using TypeCode = omg.org.CORBA.TypeCode;

namespace tecgraf.openbus.sdk {
  internal class ConnectionImpl : Connection {
    #region Fields

    private static readonly ILog Logger =
      LogManager.GetLogger(typeof (ConnectionImpl));

    private readonly string _host;
    private readonly short _port;
    private readonly IComponent _acsComponent;
    private AccessControl _acs;
    private AsymmetricKeyParameter _busKey;
    private LeaseRenewer _leaseRenewer;

    internal bool Legacy;
    internal readonly ConnectionManagerImpl Manager;

    private volatile int _sessionId;
    private readonly object _lock = new object();
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

    private const byte MajorVersion = core.v2_00.MajorVersion.ConstVal;
    private const byte MinorVersion = core.v2_00.MinorVersion.ConstVal;
    private const int SecretSize = 16;

    private readonly Codec _codec;

    private readonly int _credentialSlotId;
    private readonly int _connectionSlotId;

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
      Manager = manager;
      Legacy = legacy;
      _codec = ServerInterceptor.Instance.Codec;
      _credentialSlotId = ServerInterceptor.Instance.CredentialSlotId;
      _connectionSlotId = ServerInterceptor.Instance.ConnectionSlotId;
      _acsComponent = RemotingServices.Connect(
        typeof (IComponent),
        "corbaloc::1.0@" + _host + ":" + _port + "/" + BusObjectKey.ConstVal)
                      as IComponent;
      if (_acsComponent == null) {
        throw new OpenBusException("Não foi possível conectar ao barramento com o host e porta fornecidos.");
      }

      InternalKey = Crypto.GenerateKeyPair();

      _sessionId2Session = new LRUConcurrentDictionaryCache<int, ServerSideSession>();
      _profile2Login = new LRUConcurrentDictionaryCache<EffectiveProfile, string>();
      _outgoingLogin2Session =
        new LRUConcurrentDictionaryCache<String, ClientSideSession>();
      _joinedChainOf = new ConditionalWeakTable<Thread, CallerChain>();

      GetBusFacets();
    }

    #endregion

    #region Internal Members

    private Current GetPICurrent() {
      ORB orb = OrbServices.GetSingleton();
      Current current =
        orb.resolve_initial_references("PICurrent") as Current;
      if (current == null) {
        const string message =
          "Falha inesperada ao acessar o slot da thread corrente";
        Logger.Fatal(message);
        throw new OpenBusException(message);
      }
      return current;
    }

    private void GetBusFacets() {
      if (Login != null) {
        return;
      }
      string acsId = Repository.GetRepositoryID(typeof (AccessControl));
      string lrId = Repository.GetRepositoryID(typeof (LoginRegistry));
      string orId = Repository.GetRepositoryID(typeof (OfferRegistry));

      MarshalByRefObject acsObjRef = _acsComponent.getFacet(acsId);
      MarshalByRefObject lrObjRef = _acsComponent.getFacet(lrId);
      MarshalByRefObject orObjRef = _acsComponent.getFacet(orId);

      _acs = acsObjRef as AccessControl;
      LoginRegistry = lrObjRef as LoginRegistry;
      OfferRegistry = orObjRef as OfferRegistry;
      if ((_acs == null) || (LoginRegistry == null) || (OfferRegistry == null)) {
        Logger.Error("O serviço de controle de acesso não foi encontrado.");
        return;
      }

      Manager.ThreadRequester = this;
      BusId = _acs.busid;
      _busKey = Crypto.CreatePublicKeyFromBytes(_acs.buskey);

      if (Legacy) {
        try {
          IComponent legacy = RemotingServices.Connect(
            typeof(IComponent),
            "corbaloc::1.0@" + _host + ":" + _port + "/" + "openbus_v1_05") as IComponent;
          string legacyId = Repository.GetRepositoryID(typeof(IAccessControlService));
          if (legacy == null) {
            Legacy = false;
            Logger.Error("O serviço de controle de acesso 1.5 não foi encontrado. O suporte a conexões legadas foi desabilitado.");
          }
          else {
            MarshalByRefObject legacyObjRef = legacy.getFacet(legacyId);
            LegacyAccess = legacyObjRef as IAccessControlService;
            if (LegacyAccess == null) {
              Legacy = false;
              Logger.Error("A faceta IAccessControlService do serviço de controle de acesso 1.5 não foi encontrada. O suporte a conexões legadas foi desabilitado.");
            }
          }
        }
        catch (Exception e) {
          Legacy = false;
          Logger.Error("Erro ao tentar obter a faceta IAccessControlService da versão 1.5. O suporte a conexões legadas foi desabilitado.", e);
        }
      }
    }

    private AsymmetricCipherKeyPair InternalKey { get; set; }

    private void LoginByObject(LoginProcess login, byte[] secret) {
      if (Login != null) {
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

    internal void SetLoginsCache(LoginCache cache) {
      _loginsCache = cache;
    }

    internal LoginRegistry LoginRegistry { get; private set; }

    internal IAccessControlService LegacyAccess { get; private set; }

    #endregion

    #region Connection Members

    public OfferRegistry OfferRegistry { get; private set; }

    public string BusId { get; private set; }

    //TODO: avaliar se tem que tornar thread-safe
    public LoginInfo? Login { get; private set; }

    public void LoginByPassword(string entity, byte[] password) {
      if (Login != null) {
        throw new AlreadyLoggedInException();
      }

      try {
        Manager.IgnoreCurrentThread();
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
        Manager.ThreadRequester = this;
        Login = _acs.loginByPassword(entity, pubBytes, encrypted, out lease);
        StartLeaseRenewer(lease);
      }
      finally {
        Manager.UnignoreCurrentThread();
      }
    }

    public void LoginByCertificate(string entity, byte[] privKey) {
      if (Login != null) {
        throw new AlreadyLoggedInException();
      }

      try {
        Manager.IgnoreCurrentThread();
        byte[] challenge;
        Manager.ThreadRequester = this;
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

    public LoginProcess StartSingleSignOn(out byte[] secret) {
      byte[] challenge;
      Manager.ThreadRequester = this;
      LoginProcess login = _acs.startLoginBySingleSignOn(out challenge);
      secret = Crypto.Decrypt(InternalKey.Private, challenge);
      return login;
    }

    public void LoginBySingleSignOn(LoginProcess login, byte[] secret) {
      try {
        Manager.IgnoreCurrentThread();
        LoginByObject(login, secret);
      }
      finally {
        Manager.UnignoreCurrentThread();
      }
    }

    public bool Logout() {
      if (Login == null) {
        return false;
      }

      try {
        Manager.ThreadRequester = this;
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
        string loginId;
        try {
          loginId = current.get_slot(_connectionSlotId) as string;
        }
        catch (InvalidSlot e) {
          const string message =
            "Falha inesperada ao acessar o slot da conexão corrente";
          Logger.Fatal(message, e);
          throw new OpenBusException(message);
        }
        if ((Login == null) || (loginId == null) ||
            (!Login.Value.id.Equals(loginId))) {
          return null;
        }
        try {
          AnyCredential anyCredential =
            (AnyCredential) current.get_slot(_credentialSlotId);
          if (anyCredential.IsLegacy) {
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
            return new CallerChainImpl(BusId, callers);
          }
          CallChain chain = UnmarshalCallChain(anyCredential.Credential.chain);
          return new CallerChainImpl(BusId, chain.callers,
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

      if (!Login.HasValue) {
        Logger.Debug(
          String.Format(
            "Chamada à operação {0} cancelada devido a não existir login.",
            operation));
        throw new NO_PERMISSION(NoLoginCode.ConstVal,
                                CompletionStatus.Completed_No);
      }
      string loginId = Login.Value.id;
      string loginEntity = Login.Value.entity;

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
              if (callerChain.Callers.Length > 1) {
                lastCaller = callerChain.Callers[0].entity;
                if (callerChain.Signed.signature.Length == 0) {
                  // é uma credencial somente 1.5
                  isLegacyOnly = true;
                }
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
        if (exception.Minor == NoCredentialCode.ConstVal) {
          Logger.Warn(String.Format(
            "Servidor remoto alega falta de credencial para a chamada {0}, portanto deve ser um servidor incompatível ou com erro.",
            operation));
          throw new NO_PERMISSION(InvalidRemoteCode.ConstVal,
                                  CompletionStatus.Completed_No);
        }
        if (exception.Minor == InvalidLoginCode.ConstVal) {
          LocalLogout();
          if ((OnInvalidLoginCallback != null) &&
              (OnInvalidLoginCallback.InvalidLogin(this))) {
            Logger.Warn(String.Format(
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

    internal void ReceiveRequest(ServerRequestInfo ri,
                                 AnyCredential anyCredential) {
      string interceptedOperation = ri.operation;
      if (!anyCredential.IsLegacy) {
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
            byte[] hash = CreateCredentialHash(interceptedOperation,
                                               credential.ticket,
                                               session.Secret);
            IStructuralEquatable eqHash = hash;
            if (eqHash.Equals(credential.hash,
                              StructuralComparisons.StructuralEqualityComparer)) {
              // credencial valida
              // CheckChain pode lançar exceção com InvalidChainCode ou UnverifiedLoginCode
              CheckChain(credential.chain, credential.login);
              // insere o login no slot para a getCallerChain usar
              if (Login != null) {
                try {
                  ri.set_slot(_connectionSlotId, Login.Value.id);
                }
                catch (InvalidSlot e) {
                  const string msg =
                    "Falha ao inserir o identificador de login em seu slot.";
                  Logger.Fatal(msg, e);
                  throw new OpenBusException(msg, e);
                }
              }
              return;
            }
          }
        }
      }
      else {
        if (Login != null) {
          try {
            ri.set_slot(_connectionSlotId, Login.Value.id);
          }
          catch (InvalidSlot e) {
            const string msg =
              "Falha ao inserir o identificador de login em seu slot.";
            Logger.Fatal(msg, e);
            throw new OpenBusException(msg, e);
          }
        }
        return;
      }

      // credencial invalida por nao ter sessao conhecida, ticket inválido ou hash errado
      Logger.Fatal("Credencial inválida, enviando CredentialReset.");
      // TODO FIXME
      // Uma explicação detalhada para a linha abaixo encontra-se em um FIXME 
      // no código do interceptador servidor, no método receive_reply.
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
      Manager.ThreadRequester = this;
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

    private byte[] CreateCredentialReset(string remoteLogin) {
      if (Login != null) {
        CredentialReset reset = new CredentialReset {login = Login.Value.id};
        byte[] challenge = new byte[SecretSize];
        Random rand = new Random();
        rand.NextBytes(challenge);

        // lock para tornar esse trecho atomico
        lock (_lock) {
          AsymmetricKeyParameter pubKey;
          string entity;
          if (
            !_loginsCache.GetLoginEntity(remoteLogin, this, out entity,
                                         out pubKey)) {
            Logger.Warn("Não foi encontrada uma entrada na cache de logins para o login " + remoteLogin);
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
      string login = anyCredential.IsLegacy
                       ? anyCredential.LegacyCredential.identifier
                       : anyCredential.Credential.login;
      try {
        Manager.ThreadRequester = this;
        if (!_loginsCache.ValidateLogin(anyCredential, this)) {
          Logger.Warn(String.Format("A credencial {0} está fora da validade.",
                                    login));
          throw new NO_PERMISSION(InvalidLoginCode.ConstVal,
                                  CompletionStatus.Completed_No);
        }
      }
      catch (Exception e) {
        NO_PERMISSION noPermission = e as NO_PERMISSION;
        if ((noPermission != null) &&
            (noPermission.Minor == InvalidLoginCode.ConstVal)) {
          Logger.Fatal(
            "Este servidor foi deslogado do barramento durante a interceptação desta requisição",
            e);
        }
        Logger.Fatal(
          "Não foi possível validar a credencial. Erro: " + e.Message, e);
        throw new NO_PERMISSION(UnverifiedLoginCode.ConstVal,
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
  }
}