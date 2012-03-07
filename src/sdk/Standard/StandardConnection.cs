using System;
using System.Collections.Concurrent;
using System.Runtime.Remoting;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Ch.Elca.Iiop.Idl;
using log4net;
using omg.org.CORBA;
using omg.org.IOP;
using omg.org.PortableInterceptor;
using scs.core;
using tecgraf.openbus.core.v2_00.credential;
using tecgraf.openbus.core.v2_00.services.access_control;
using tecgraf.openbus.core.v2_00.services.offer_registry;
using tecgraf.openbus.sdk.exceptions;
using tecgraf.openbus.sdk.Security;
using tecgraf.openbus.sdk.lease;
using Encoding = omg.org.IOP.Encoding;
using TypeCode = omg.org.CORBA.TypeCode;

namespace tecgraf.openbus.sdk.Standard {
  internal class StandardConnection : Connection {
    #region Fields

    private static readonly ILog Logger =
      LogManager.GetLogger(typeof (StandardConnection));

    private readonly string _host;
    private readonly short _port;
    private readonly IComponent _acsComponent;
    private readonly AccessControl _acs;
    private readonly LoginRegistry _lr;
    private readonly RSACryptoServiceProvider _busKey;
    private readonly X509Certificate2 _certificate;
    private LoginInfo? _login;
    private LeaseRenewer _leaseRenewer;

    //TODO: avaliar a melhor forma de armazenar a chave. String é uma boa opção? Se fosse usar o array de bytes direto eu teria q criar uma classe com metodos de comparacao que criasse um hash, para nao ficar muito cara a comparacao...
    //TODO: caches ainda nao tem nenhuma politica de remocao ou de tamanho maximo
    private readonly ConcurrentDictionary<string, Session>
      _outgoingLogin2Session;

    private readonly ConcurrentDictionary<String, string> _profile2Login;
    private int _sessionId = 1;
    private readonly ConcurrentDictionary<int, Session> _sessionId2Session;

    private readonly ConcurrentDictionary<string, RSACryptoServiceProvider>
      _login2PubKey;

    private readonly object _lock = new object();

    /// <summary>
    /// Representam a identificação dos "service contexts" (contextos) utilizados
    /// para transporte de credenciais em requisições de serviço.
    /// </summary>
    private const int ContextId = CredentialContextId.ConstVal;

    private const int PrevContextId = 1234;

    //TODO: Maia vai criar constantes na IDL para os 3 casos abaixo
    private const byte MajorVersion = core.v2_00.MajorVersion.ConstVal;
    private const byte MinorVersion = core.v2_00.MinorVersion.ConstVal;
    private readonly const int SecretSize = 16;

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
      _codec = GetCodec();
      _acsComponent = RemotingServices.Connect(typeof (IComponent),
                                               "corbaloc::1.0@" + _host + ":" +
                                               _port + "/" +
                                               core.v2_00.BusObjectKey.ConstVal)
                      as
                      IComponent;

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
      _busKey = Crypto.ReadPrivateKey(_acs.buskey);

      _certificate = Crypto.NewCertificate();
      PrivateKey = Crypto.GetPrivateKey(_certificate);
      PublicKey = Crypto.GetPublicKey(_certificate);

      _sessionId2Session = new ConcurrentDictionary<int, Session>();
      _login2PubKey =
        new ConcurrentDictionary<string, RSACryptoServiceProvider>();
      _profile2Login = new ConcurrentDictionary<String, string>();
      _outgoingLogin2Session = new ConcurrentDictionary<String, Session>();
      //TODO: Adicionar cache de logins
    }

    #endregion

    #region Internal Members

    private RSACryptoServiceProvider PublicKey { get; set; }

    private RSACryptoServiceProvider PrivateKey { get; set; }

    internal bool IsLoggedIn() {
      return (_login == null);
    }

    private void LoginByObject(LoginProcess login, byte[] secret) {
      if (IsLoggedIn()) {
        login.cancel();
        throw new AlreadyLoggedInException();
      }

      Codec codec = GetCodec();

      byte[] encrypted;
      byte[] pubBlob = PublicKey.ExportCspBlob(false);
      try {
        //encode answer and hash of public key
        LoginAuthenticationInfo info = new LoginAuthenticationInfo {
                                                                     data =
                                                                       secret,
                                                                     hash =
                                                                       SHA256.
                                                                       Create().
                                                                       ComputeHash
                                                                       (pubBlob)
                                                                   };
        encrypted = Crypto.Encrypt(_busKey, codec.encode_value(info));
      }
      catch {
        login.cancel();
        const string msg = "Erro na codificação das informações de login.";
        Logger.Fatal(msg);
        throw;
      }

      int lease;
      Login = login.login(pubBlob, encrypted, out lease);
      StartLeaseRenewer();
    }

    private Codec GetCodec() {
      OrbServices orb = OrbServices.GetSingleton();
      CodecFactory factory =
        orb.resolve_initial_references("CodecFactory") as CodecFactory;
      if (factory == null) {
        throw new OpenbusException("CodecFactory is null, cannot encode data.");
      }
      Encoding encode = new Encoding(ENCODING_CDR_ENCAPS.ConstVal, 1, 2);
      Codec codec = factory.create_codec(encode);
      return codec;
    }

    private void LocalLogout() {
      _login = null;
      StopLeaseRenewer();
      //TODO: resetar caches qdo forem implementados
    }

    private void StartLeaseRenewer() {
      _leaseRenewer = new LeaseRenewer(this, _acs);
      _leaseRenewer.Start();
      Logger.Debug("Thread de renovação de lease está ativa. Lease = "
        + _leaseRenewer.GetLease() + " segundos.");
    }

    private void StopLeaseRenewer() {
      if (_leaseRenewer != null) {
        _leaseRenewer.Finish();
        _leaseRenewer = null;
        Logger.Debug("Thread de renovação de lease desativada.");
      }
    }

    #endregion

    #region Connection Members

    public OfferRegistry OfferRegistry { get; private set; }

    public string BusId { get; private set; }

    public LoginInfo? Login { get; private set; }

    public void LoginByPassword(string entity, byte[] password) {
      if (IsLoggedIn()) {
        throw new AlreadyLoggedInException();
      }

      Codec codec = GetCodec();

      byte[] encrypted;
      byte[] pubBlob = PublicKey.ExportCspBlob(false);
      try {
        //encode password and hash of public key
        LoginAuthenticationInfo info = new LoginAuthenticationInfo {
                                                                     data =
                                                                       password,
                                                                     hash =
                                                                       SHA256.
                                                                       Create().
                                                                       ComputeHash
                                                                       (pubBlob)
                                                                   };
        encrypted = Crypto.Encrypt(_busKey, codec.encode_value(info));
      }
      catch {
        const string msg = "Erro na codificação das informações de login.";
        Logger.Fatal(msg);
        throw;
      }

      int lease;
      Login = _acs.loginByPassword(entity, pubBlob, encrypted, out lease);
      StartLeaseRenewer();
    }

    public void LoginByCertificate(string entity, byte[] privKey) {
      if (IsLoggedIn()) {
        throw new AlreadyLoggedInException();
      }
      byte[] challenge;
      LoginProcess login = _acs.startLoginByCertificate(entity,
                                                        out
                                                          challenge);
      byte[] answer = Crypto.Decrypt(_busKey, challenge);
      LoginByObject(login, answer);
    }

    public LoginProcess StartSingleSignOn(out byte[] secret) {
      byte[] challenge;
      LoginProcess login = _acs.startLoginBySingleSignOn(out challenge);
      secret = Crypto.Decrypt(PrivateKey, challenge);
      return login;
    }

    public void LoginBySingleSignOn(LoginProcess login, byte[] secret) {
      LoginByObject(login, secret);
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
      throw new NotImplementedException();
    }

    public CallerChain GetCallerChain() {
      throw new NotImplementedException();
    }

    public void ExitChain() {
      throw new NotImplementedException();
    }

    public CallerChain GetJoinedChain() {
      throw new NotImplementedException();
    }

    public void Close() {
      try {
        Logout();
      }
      catch (Exception e) {
        Logger.Warn(String.Format("Erro capturado ao fechar conexão: {0}", e.Message));
      }
      finally {
        StandardOpenbus.RemoveConnection();
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
      int sessionId = 0;
      int ticket = 0;
      byte[] secret = new byte[0];

      string profile = ri.effective_profile.tag +
                       ri.effective_profile.profile_data.ToString();
      string remoteLogin;
      _profile2Login.TryGetValue(profile, out remoteLogin);

      Session session;
      if (_outgoingLogin2Session.TryGetValue(remoteLogin, out session)) {
        sessionId = session.Id;
        ticket = session.Ticket + 1;
        secret = new byte[session.Secret.Length];
        session.Secret.CopyTo(secret, 0);
      }

      try {
        byte[] hash;
        SignedCallChain chain;
        if (sessionId != 0) {
          hash = CreateCredentialHash(operation, ticket, secret, ri.request_id);
          //TODO: codigo abaixo assume que nao existe cache de cadeias ainda
          chain = CreateCredentialSignedCallChain(remoteLogin);
          Logger.Info(
            String.Format("Chamada à operação {0} no servidor de login {1}.",
                          operation, remoteLogin));
        }
        else {
          // Cria credencial inválida para iniciar o handshake e obter uma nova sessão
          hash = CreateInvalidCredentialHash();
          chain = CreateInvalidCredentialSignedCallChain();
          Logger.Info(
            String.Format(
              "Inicializando sessão de credencial para requisitar a operação {0} no login {1}.",
              operation, remoteLogin));
        }

        byte[] value = CreateAndEncodeCredential(loginId, sessionId, ticket,
                                                 hash,
                                                 chain);
        ServiceContext serviceContext = new ServiceContext(ContextId, value);
        ri.add_request_service_context(serviceContext, false);
      }
      catch (NO_PERMISSION e) {
        if (e.Minor == InvalidLoginCode.ConstVal) {
          Logger.Fatal(
            "Este cliente foi deslogado do barramento durante a interceptação desta requisição.",
            e);
          if ((OnInvalidLoginCallback != null) && (OnInvalidLoginCallback.InvalidLogin(this))) {
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
      String exceptionId = ri.received_exception_id;
      Logger.Info(String.Format(
        "A exceção '{0}' foi interceptada ao tentar realizar a chamada {1}.",
        exceptionId, operation));

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
        return;
      }

      CredentialReset requestReset = ReadCredentialReset(ri, exception);
      string remoteLogin = requestReset.login;
      string profile = ri.effective_profile.tag +
                       ri.effective_profile.profile_data.ToString();
      _profile2Login.TryAdd(profile, remoteLogin);

      Session session;
      if (_outgoingLogin2Session.TryGetValue(remoteLogin, out session)) {
        Logger.Info(
          String.Format(
            "Reuso de sessão de credencial {0} ao tentar requisitar a operação {1} ao login {2}.",
            session.Id, operation, remoteLogin));
      }
      else {
        int sessionId = requestReset.session;
        byte[] secret = Crypto.Decrypt(PrivateKey,
                                       requestReset.challenge);
        _outgoingLogin2Session.TryAdd(remoteLogin,
                                      new Session(sessionId, secret,
                                                  remoteLogin));
        Logger.Info(
          String.Format(
            "Início de sessão de credencial {0} ao tentar requisitar a operação {1} ao login {2}.",
            session.Id, operation, remoteLogin));
      }
      // pede que a chamada original seja relançada
      throw new ForwardRequest(ri.target);
    }

    internal void ReceiveRequest(ServerRequestInfo ri) {
      String interceptedOperation = ri.operation;
      Logger.Info(String.Format(
        "A operação '{0}' foi interceptada no servidor.", interceptedOperation));

      ServiceContext serviceContext;
      try {
        serviceContext = ri.get_request_service_context(ContextId);
      }
      catch (BAD_PARAM) {
        Logger.Warn(String.Format(
          "A chamada à operação '{0}' não possui credencial.",
          interceptedOperation));
        throw new NO_PERMISSION(InvalidLoginCode.ConstVal,
                                CompletionStatus.Completed_No);
      }
      if (serviceContext.context_data == null) {
        Logger.Fatal(String.Format(
          "A chamada à operação '{0}' não possui credencial.",
          interceptedOperation));
        throw new NO_PERMISSION(InvalidLoginCode.ConstVal,
                                CompletionStatus.Completed_No);
      }

      CredentialData credential = UnmarshalCredential(serviceContext);
      Logger.Info(String.Format("A operação '{0}' possui credencial.",
                                interceptedOperation));
      if (!credential.bus.Equals(BusId)) {
        Logger.Fatal(
          String.Format(
            "A identificação do barramento está errada. O valor recebido foi '{0}' e o esperado era '{1}'.",
            BusId, credential.bus));
        throw new NO_PERMISSION(UnknownBusCode.ConstVal,
                                CompletionStatus.Completed_No);
      }

      // CheckValidity lança exceção caso a validade tenha expirado
      CheckValidity(credential);

      byte[] secret = new byte[0];
      int ticket = 0;
      Session session;
      if (_sessionId2Session.TryGetValue(credential.session, out session)) {
        secret = session.Secret;
        ticket = session.Ticket;
      }

      if (secret.Length > 0) {
        byte[] hash = CreateCredentialHash(ri.operation, ticket, secret,
                                           ri.request_id);
        if (hash.Equals(credential.hash)) {
          // credencial valida
          // CheckChain pode lançar exceção com InvalidChainCode ou UnverifiedLoginCode
          CheckChain(credential.chain, credential.login);
          return;
        }
      }
      // credendial invalida por nao ter sessao conhecida ou hash errado
      // CreateCredentialReset pode lançar exceção com UnverifiedLoginCode
      byte[] value = CreateCredentialReset(credential.login);
      ServiceContext replyServiceContext = new ServiceContext(ContextId,
                                                              value);
      ri.add_reply_service_context(replyServiceContext, false);
      Logger.Fatal("Credencial inválida, enviando CredentialReset.");
      throw new NO_PERMISSION(InvalidCredentialCode.ConstVal,
                              CompletionStatus.Completed_No);
    }

    private byte[] CreateInvalidCredentialHash() {
      return new byte[] {0};
    }

    private SignedCallChain CreateCredentialSignedCallChain(string remoteLogin) {
      //TODO: se for o barramento, retornar inválida ou "nula"?
      return !remoteLogin.Equals(BusId)
               ? _acs.signChainFor(remoteLogin)
               : CreateInvalidCredentialSignedCallChain();
    }

    private SignedCallChain CreateInvalidCredentialSignedCallChain() {
      return new SignedCallChain(new byte[] {0}, new byte[0]);
    }

    private byte[] CreateAndEncodeCredential(string loginId, int sessionId,
                                             int ticket, byte[] hash,
                                             SignedCallChain chain) {
      CredentialData data = new CredentialData(BusId, loginId, sessionId,
                                               ticket,
                                               hash, chain);
      byte[] value = _codec.encode_value(data);
      return value;
    }

    private CredentialReset ReadCredentialReset(ClientRequestInfo ri,
                                                NO_PERMISSION exception) {
      CredentialReset requestReset;

      try {
        ServiceContext serviceContext =
          ri.get_request_service_context(ContextId);

        OrbServices orb = OrbServices.GetSingleton();
        Type resetType = typeof (CredentialReset);
        TypeCode resetTypeCode =
          orb.create_interface_tc(
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

    private CredentialData UnmarshalCredential(ServiceContext serviceContext) {
      OrbServices orb = OrbServices.GetSingleton();
      Type credentialType = typeof (CredentialData);
      TypeCode credentialTypeCode =
        orb.create_interface_tc(Repository.GetRepositoryID(credentialType),
                                credentialType.Name);

      byte[] data = serviceContext.context_data;
      return (CredentialData) _codec.decode_value(data, credentialTypeCode);
    }

    private byte[] CreateCredentialReset(string remoteLogin) {
      if (Login != null) {
        CredentialReset reset = new CredentialReset();
        reset.login = Login.Value.id;
        reset.challenge = new byte[SecretSize];
        Random rand = new Random();
        rand.NextBytes(reset.challenge);
        RSACryptoServiceProvider pubKey;

        // lock para tornar esse trecho atomico
        lock (_lock) {
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
            pubKey = new RSACryptoServiceProvider();
            pubKey.ImportCspBlob(key);
            _login2PubKey.TryAdd(remoteLogin, pubKey);
          }
          reset.challenge = Crypto.Encrypt(pubKey, reset.challenge);

          reset.session = _sessionId;
          Session session = new Session(reset.session, reset.challenge,
                                        remoteLogin);
          if (_sessionId2Session.TryAdd(reset.session, session)) {
            _sessionId++;
          }
        }
        return _codec.encode_value(reset);
      }
      // Este servidor não está logado no barramento
      Logger.Fatal("Este servidor não está logado no barramento");
      throw new NO_PERMISSION(UnverifiedLoginCode.ConstVal,
                              CompletionStatus.Completed_No);
    }

    private void CheckValidity(CredentialData credential) {
      int validity;
      try {
        validity = _lr.getValidity(new[] {credential.login})[0];
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
        Logger.Warn(String.Format("A credencial {0}:{1} está fora da validade.",
                                  credential.bus, credential.login));
        throw new NO_PERMISSION(InvalidLoginCode.ConstVal,
                                CompletionStatus.Completed_No);
      }
      Logger.Info(String.Format("A credencial {0}:{1} está na validade.",
                                credential.bus, credential.login));
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
        if (!chain.target.Equals(Login.Value) ||
            (!chain.callers[chain.callers.Length - 1].id.Equals(callerId)) ||
            (!_busKey.VerifyData(signed.encoded, SHA256.Create(),
                                 signed.signature))) {
          Logger.Fatal("Credencial inválida, enviando CredentialReset.");
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
                                        byte[] secret, int requestId) {
      UTF8Encoding utf8 = new UTF8Encoding();
      // 2 bytes para versao, 16 para o segredo, 4 para o ticket em little endian, 4 para o request id em little endian e X para a operacao.
      int size = 2 + secret.Length + 4 + 4 + utf8.GetByteCount(operation);
      byte[] hash = new byte[size];
      hash[0] = MajorVersion;
      hash[1] = MinorVersion;
      int index = 2;
      secret.CopyTo(hash, index);
      index += secret.Length;
      byte[] bTicket = BitConverter.GetBytes(ticket);
      byte[] bRequestId = BitConverter.GetBytes(requestId);
      if (!BitConverter.IsLittleEndian) {
        Array.Reverse(bTicket);
        Array.Reverse(bRequestId);
      }
      bTicket.CopyTo(hash, index);
      index += 4;
      bRequestId.CopyTo(hash, index);
      byte[] bOperation = utf8.GetBytes(operation);
      index += 4;
      bOperation.CopyTo(hash, index);
      return SHA256.Create().ComputeHash(hash);
    }

    private class Session {
      public Session(int id, byte[] secret, string remoteLogin) {
        Id = id;
        Secret = secret;
        RemoteLogin = remoteLogin;
        Ticket = -1;
      }

      public string RemoteLogin { get; private set; }

      public byte[] Secret { get; private set; }

      public int Id { get; private set; }

      public int Ticket { get; private set; }
    }

    #endregion
  }
}