﻿using System;
using System.Collections;
using System.Security.Cryptography;
using System.Threading;
using Ch.Elca.Iiop.Idl;
using log4net;
using omg.org.CORBA;
using omg.org.IOP;
using omg.org.PortableInterceptor;
using Org.BouncyCastle.Crypto;
using scs.core;
using tecgraf.openbus.caches;
using tecgraf.openbus.core.v2_1;
using tecgraf.openbus.core.v2_1.credential;
using tecgraf.openbus.core.v2_1.services;
using tecgraf.openbus.core.v2_1.services.access_control;
using tecgraf.openbus.core.v2_1.services.offer_registry;
using tecgraf.openbus.exceptions;
using tecgraf.openbus.interceptors;
using tecgraf.openbus.lease;
using tecgraf.openbus.security;
using Current = omg.org.PortableInterceptor.Current;
using Encoding = System.Text.Encoding;
using TypeCode = omg.org.CORBA.TypeCode;

namespace tecgraf.openbus {
  internal class ConnectionImpl : Connection {
    #region Fields

    private static readonly ILog Logger =
      LogManager.GetLogger(typeof (ConnectionImpl));

    private readonly IComponent _busIC;
    private AccessControl _acs;
    private string _busId;
    private AsymmetricKeyParameter _busKey;
    private LeaseRenewer _leaseRenewer;

    internal readonly OpenBusContextImpl Context;

    private volatile int _sessionId;

    private readonly ReaderWriterLockSlim _sessionIdLock =
      new ReaderWriterLockSlim();

    private LoginInfo? _login;
    private LoginInfo? _invalidLogin;
    private InvalidLoginCallback _onInvalidLoginCallback;
    private readonly AsymmetricCipherKeyPair _internalKeyPair;
    private OfferRegistry _offers;
    private LoginRegistry _loginRegistry;

    private readonly ReaderWriterLockSlim _loginLock =
      new ReaderWriterLockSlim();

    // client caches
    private readonly LRUConcurrentDictionaryCache<EffectiveProfile, string>
      _profile2Login;

    private readonly LRUConcurrentDictionaryCache<string, ClientSideSession>
      _outgoingLogin2Session;

    // server caches
    private readonly LRUConcurrentDictionaryCache<int, ServerSideSession>
      _sessionId2Session;

    private LoginCache _loginsCache;

    /// <summary>
    ///   Representam a identificação dos "service contexts" (contextos) utilizados
    ///   para transporte de credenciais em requisições de serviço.
    /// </summary>
    private const int ContextId = CredentialContextId.ConstVal;

    private const byte MajorVersion = core.v2_1.MajorVersion.ConstVal;
    private const byte MinorVersion = core.v2_1.MinorVersion.ConstVal;
    private const int SecretSize = 16;

    private readonly Codec _codec;

    private readonly int _chainSlotId;
    private readonly int _loginSlotId;
    private readonly int _noInvalidLoginHandlingSlotId;

    private readonly byte[] _nullHash = new byte[HashValueSize.ConstVal];
    private readonly SignedData _invalidSignedData = new SignedData(new byte[EncryptedBlockSize.ConstVal], new byte[0]);

    private const string BusPubKeyError =
      "Erro ao encriptar as informações de login com a chave pública do barramento.";

    private const string InternalPrivKeyError =
      "Erro ao decriptar as informações de login com a chave privada interna.";

    #endregion

    #region Constructors

    internal ConnectionImpl(IComponent iComponent, OpenBusContextImpl context,
      PrivateKeyImpl accessKey) {
      _busIC = iComponent;
      ORB = OrbServices.GetSingleton();
      Context = context;
      _codec = InterceptorsInitializer.Codec;
      _chainSlotId = ServerInterceptor.Instance.ChainSlotId;
      _loginSlotId = ClientInterceptor.Instance.LoginSlotId;
      _noInvalidLoginHandlingSlotId = ClientInterceptor.Instance.NoInvalidLoginHandlingSlotId;

      _internalKeyPair = accessKey != null
        ? accessKey.Pair
        : Crypto.GenerateKeyPair();

      _sessionId2Session =
        new LRUConcurrentDictionaryCache<int, ServerSideSession>();
      _profile2Login =
        new LRUConcurrentDictionaryCache<EffectiveProfile, string>();
      _outgoingLogin2Session =
        new LRUConcurrentDictionaryCache<String, ClientSideSession>();

      _login = null;
      _invalidLogin = null;
    }

    #endregion

    #region Internal Members

    private void GetBusFacets() {
      const string connErrorMessage =
        "Não foi possível conectar ao barramento com o host e porta fornecidos.";
      try {
        string acsId = Repository.GetRepositoryID(typeof (AccessControl));
        string lrId = Repository.GetRepositoryID(typeof (LoginRegistry));
        string orId = Repository.GetRepositoryID(typeof (OfferRegistry));

        MarshalByRefObject acsObjRef = _busIC.getFacet(acsId);
        MarshalByRefObject lrObjRef = _busIC.getFacet(lrId);
        MarshalByRefObject orObjRef = _busIC.getFacet(orId);

        AccessControl localAcs;
        _loginLock.EnterWriteLock();
        try {
          localAcs = _acs = acsObjRef as AccessControl;
          _loginRegistry = lrObjRef as LoginRegistry;
          _offers = orObjRef as OfferRegistry;
          if ((_acs == null) || (_loginRegistry == null) || (_offers == null)) {
            const string msg =
              "As facetas de controle de acesso, registro de logins e/ou registro de ofertas não foram encontradas.";
            throw new ServiceFailure {message = msg};
          }
          _loginsCache = new LoginCache(this);
        }
        finally {
          _loginLock.ExitWriteLock();
        }
        AsymmetricKeyParameter busKey;
        string busId = GetBusIdAndKey(localAcs, out busKey);
        _loginLock.EnterWriteLock();
        try {
          _busId = busId;
          _busKey = busKey;
        }
        finally {
          _loginLock.ExitWriteLock();
        }
      }
      catch (Exception e) {
        Logger.Error(connErrorMessage, e);
        throw;
      }
    }

    private string GetBusIdAndKey(AccessControl acs,
      out AsymmetricKeyParameter key) {
      key = Crypto.CreatePublicKeyFromBytes(acs.buskey);
      return acs.busid;
    }

    private void LoginByObject(LoginProcess login, byte[] secret) {
      bool loggedIn;
      AsymmetricKeyParameter busKey;
      _loginLock.EnterReadLock();
      try {
        loggedIn = _login.HasValue;
        busKey = _busKey;
      }
      finally {
        _loginLock.ExitReadLock();
      }
      if (loggedIn) {
        login.cancel();
        throw new AlreadyLoggedInException();
      }

      byte[] encrypted;
      byte[] pubBytes = Crypto.GetPublicKeyInBytes(_internalKeyPair.Public);
      try {
        //encode answer and hash of public key
        LoginAuthenticationInfo info =
          new LoginAuthenticationInfo
          {data = secret, hash = SHA256.Create().ComputeHash(pubBytes)};
        encrypted = Crypto.Encrypt(busKey, _codec.encode_value(info));
      }
      catch (InvalidCipherTextException) {
        login.cancel();
        Logger.Error(BusPubKeyError);
        throw new ServiceFailure {message = BusPubKeyError};
      }
      catch (Exception) {
        login.cancel();
        Logger.Error("Erro na codificação das informações de login.");
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
        throw new ServiceFailure {
          message =
            "Erro na codificação da chave pública do barramento."
        };
      }
      catch (Exception e) {
        Logger.Error(e);
        throw;
      }
      _loginLock.EnterWriteLock();
      try {
        LocalLogin(l, lease);
      }
      finally {
        _loginLock.ExitWriteLock();
      }
    }

    // NÃO É THREAD-SAFE!! Tem que proteger ao chamar. Motivo: evitar ter de ativar a política de recursão de locks.
    private void LocalLogin(LoginInfo login, int lease) {
      if (_login.HasValue) {
        throw new AlreadyLoggedInException();
      }
      _login = login;
      _invalidLogin = null;
      _leaseRenewer = new LeaseRenewer(this, lease);
      _leaseRenewer.Start();
      Logger.Debug("Thread de renovação de lease está ativa. Lease = "
                   + lease + " segundos.");
    }

    // NÃO É THREAD-SAFE!! Tem que proteger ao chamar. Motivo: evitar ter de ativar a política de recursão de locks.
    private void LocalLogout() {
      if (_leaseRenewer != null) {
        _leaseRenewer.Finish();
        _leaseRenewer = null;
        Logger.Debug("Thread de renovação de lease desativada.");
      }
      _login = null;
      _loginsCache = null;
      _acs = null;
      _loginRegistry = null;
      _offers = null;
      _busId = null;
      _busKey = null;
      _outgoingLogin2Session.Clear();
      _profile2Login.Clear();
      _sessionId2Session.Clear();
    }

    internal OfferRegistry Offers {
      get {
        _loginLock.EnterReadLock();
        try {
          return _offers;
        }
        finally {
          _loginLock.ExitReadLock();
        }
      }
    }

    internal LoginRegistry LoginRegistry {
      get {
        _loginLock.EnterReadLock();
        try {
          return _loginRegistry;
        }
        finally {
          _loginLock.ExitReadLock();
        }
      }
    }

    internal AccessControl Acs {
      get {
        _loginLock.EnterReadLock();
        try {
          return _acs;
        }
        finally {
          _loginLock.ExitReadLock();
        }
      }
    }

    internal AsymmetricKeyParameter BusKey {
      get {
        _loginLock.EnterReadLock();
        try {
          return _busKey;
        }
        finally {
          _loginLock.ExitReadLock();
        }
      }
    }

    #endregion

    #region Connection Members

    public OrbServices ORB { get; private set; }

    public string BusId {
      get {
        _loginLock.EnterReadLock();
        try {
          return _busId;
        }
        finally {
          _loginLock.ExitReadLock();
        }
      }
    }

    public LoginInfo? Login {
      get {
        _loginLock.EnterReadLock();
        try {
          return _login;
        }
        finally {
          _loginLock.ExitReadLock();
        }
      }
    }

    public void LoginByPassword(string entity, byte[] password) {
      if (entity == null || password == null) {
        throw new ArgumentException("A entidade e a senha não podem ser nulas.");
      }

      Context.IgnoreCurrentThread();
      try {
        byte[] encrypted;
        byte[] pubBytes = Crypto.GetPublicKeyInBytes(_internalKeyPair.Public);

        GetBusFacets();

        AsymmetricKeyParameter busKey;
        AccessControl localAcs;
        _loginLock.EnterReadLock();
        try {
          if (_login.HasValue) {
            throw new AlreadyLoggedInException();
          }
          busKey = _busKey;
          localAcs = _acs;
        }
        finally {
          _loginLock.ExitReadLock();
        }

        try {
          LoginAuthenticationInfo info =
            new LoginAuthenticationInfo
            {data = password, hash = SHA256.Create().ComputeHash(pubBytes)};
          encrypted = Crypto.Encrypt(busKey, _codec.encode_value(info));
        }
        catch (InvalidCipherTextException) {
          Logger.Error(BusPubKeyError);
          throw new ServiceFailure {message = BusPubKeyError};
        }
        catch {
          Logger.Error("Erro na codificação das informações de login.");
          throw;
        }

        int lease;
        LoginInfo l = localAcs.loginByPassword(entity, pubBytes, encrypted,
          out lease);
        _loginLock.EnterWriteLock();
        try {
          LocalLogin(l, lease);
        }
        finally {
          _loginLock.ExitWriteLock();
        }
      }
      finally {
        Context.UnignoreCurrentThread();
      }
    }

    public void LoginByCertificate(string entity, PrivateKey privateKey) {
      if (entity == null) {
        throw new ArgumentException("A entidade não pode ser nula.");
      }
      PrivateKeyImpl temp = privateKey as PrivateKeyImpl;
      if (temp == null) {
        throw new ArgumentException(
          "A chave privada fornecida deve ser gerada pela API do SDK do OpenBus.");
      }
      AsymmetricKeyParameter key = temp.Pair.Private;

      Context.IgnoreCurrentThread();
      try {
        GetBusFacets();

        AccessControl localAcs;
        _loginLock.EnterReadLock();
        try {
          if (_login.HasValue) {
            throw new AlreadyLoggedInException();
          }
          localAcs = _acs;
        }
        finally {
          _loginLock.ExitReadLock();
        }

        byte[] challenge;
        LoginProcess login = localAcs.startLoginByCertificate(entity,
          out
            challenge);
        byte[] answer;
        try {
          answer = Crypto.Decrypt(key, challenge);
        }
        catch (InvalidCipherTextException) {
          Logger.Error(
            "Erro ao decodificar o desafio com a chave privada fornecida.");
          throw new AccessDenied();
        }
        catch (DataLengthException) {
          Logger.Error(
            "Erro ao decodificar o desafio com a chave privada fornecida. O tamanho do dado é maior que a chave.");
          throw new AccessDenied();
        }
        LoginByObject(login, answer);
      }
      finally {
        Context.UnignoreCurrentThread();
      }
    }

    public SharedAuthSecret StartSharedAuth() {
      LoginProcess login = null;
      byte[] secret;
      Connection prev = Context.SetCurrentConnection(this);
      AccessControl localAcs;
      string busId;
      _loginLock.EnterReadLock();
      try {
        localAcs = _acs;
        busId = _busId;
      }
      finally {
        _loginLock.ExitReadLock();
      }
      try {
        byte[] challenge;
        login = localAcs.startLoginBySharedAuth(out challenge);
        secret = Crypto.Decrypt(_internalKeyPair.Private, challenge);
      }
      catch (InvalidCipherTextException) {
        if (login != null) {
          login.cancel();
        }
        Logger.Error(InternalPrivKeyError);
        throw new OpenBusInternalException(InternalPrivKeyError);
      }
      catch (Exception) {
        if (login != null) {
          login.cancel();
        }
        throw;
      }
      finally {
        Context.SetCurrentConnection(prev);
      }
      return new SharedAuthSecretImpl(busId, login, secret);
    }

    public void LoginBySharedAuth(SharedAuthSecret secret) {
      SharedAuthSecretImpl sharedAuth = secret as SharedAuthSecretImpl;
      if (sharedAuth == null || sharedAuth.Attempt == null || sharedAuth.Secret == null) {
        throw new ArgumentException("O segredo fornecido é inválido.");
      }

      Context.IgnoreCurrentThread();
      try {
        GetBusFacets();
        LoginByObject(sharedAuth.Attempt, sharedAuth.Secret);
      }
      finally {
        Context.UnignoreCurrentThread();
      }
    }

    public bool Logout() {
      AccessControl localAcs;
      string id;
      string entity;
      string busId;
      _loginLock.EnterWriteLock();
      try {
        if (!_login.HasValue) {
          _invalidLogin = null;
          return false;
        }
        id = _login.Value.id;
        entity = _login.Value.entity;
        localAcs = _acs;
        busId = _busId;
      }
      finally {
        _loginLock.ExitWriteLock();
      }

      CallerChain prevChain = Context.JoinedChain;
      Connection prev = Context.SetCurrentConnection(this);
      try {
        Context.ExitChain();
        // armazena informação no PICurrent de que essa chamada não deve chamar a callback de login inválido
        Current current = Context.GetPICurrent();
        current.set_slot(_noInvalidLoginHandlingSlotId, true);
        localAcs.logout();
        current.set_slot(_noInvalidLoginHandlingSlotId, null);
      }
      catch (InvalidSlot e) {
        Logger.Fatal("Falha inesperada ao acessar o slot de tratamento de login inválido", e);
        throw;
      }
      catch (AbstractCORBASystemException e) {
        // ignora invalidlogin
        NO_PERMISSION ex = e as NO_PERMISSION;
        if ((ex != null) && (ex.Minor == InvalidLoginCode.ConstVal)) {
          // caso receba um invalidlogin, o logout no barramento não é necessário, logo deve retornar true.
          Logger.Debug(String.Format(
            "A chamada remota logout resultou em um login inválido e portanto apenas o logout local será realizado. busid {0} login {1} entidade {2}.",
            busId, id, entity));
          return true;
        }
        // Não lança exceções corba, retorna falso em caso de erro.
        // serei deslogado do barramento após o próximo lease
        Logger.Warn(String.Format(
          "Erro durante chamada remota logout: busid {0} login {1} entidade {2}.\nExceção: {3}",
          busId, id, entity, e));
        return false;
      }
      finally {
        Context.SetCurrentConnection(prev);
        Context.JoinChain(prevChain);
        _loginLock.EnterWriteLock();
        try {
          LocalLogout();
        }
        finally {
          _loginLock.ExitWriteLock();
        }
      }
      return true;
    }

    public InvalidLoginCallback OnInvalidLogin {
      get {
        _loginLock.EnterReadLock();
        try {
          return _onInvalidLoginCallback;
        }
        finally {
          _loginLock.ExitReadLock();
        }
      }
      set {
        _loginLock.EnterWriteLock();
        try {
          _onInvalidLoginCallback = value;
        }
        finally {
          _loginLock.ExitWriteLock();
        }
      }
    }

    #endregion

    #region Interceptor Methods

    internal void SendRequest(ClientRequestInfo ri) {
      string operation = ri.operation;
      Logger.Info(
        String.Format(
          "Interceptador cliente iniciando tentativa de chamada à operação {0}.",
          operation));

      string errMsg =
        String.Format(
          "Chamada à operação {0} cancelada devido a não existir login.",
          operation);
      LoginInfo login = GetLoginOrThrowNoLogin(errMsg, null);
      Logger.Debug(
        String.Format(
          "Login sendo armazenado no PICurrent: entidade {0} e id {1}.",
          login.entity, login.id));

      // armazena login no PICurrent para obter no caso de uma exceção
      Current current = Context.GetPICurrent();
      try {
        current.set_slot(_loginSlotId, login);
      }
      catch (InvalidSlot e) {
        Logger.Fatal("Falha inesperada ao acessar o slot do login corrente", e);
        throw;
      }

      int sessionId = -1;
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
          Logger.Debug(String.Format("Reutilizando sessão {0} com ticket {1}.",
            sessionId, ticket));
          session.Ticket++;
        }
      }

      try {
        byte[] hash;
        SignedData chain;
        if (sessionId >= 0) {
          Logger.Debug(
            String.Format(
              "Criando hash com operação {0}, ticket {1} e segredo {2}",
              operation, ticket, BitConverter.ToString(secret)));
          hash = CreateCredentialHash(operation, ticket, secret);
          Logger.Debug("Hash criado: " + BitConverter.ToString(hash));
          // CreateCredentialSignedCallChain pode mudar o login
          chain = CreateCredentialSignedCallChain(remoteLogin);
          login = GetLoginOrThrowNoLogin(errMsg, null);
        }
        else {
          // Não encontrou sessão, volta o sessionId para zero por ser o valor padrão para o credential reset
          sessionId = 0;
          // Cria credencial inválida para iniciar o handshake e obter uma nova sessão
          hash = _nullHash;
          chain = _invalidSignedData;
          Logger.Debug(
            String.Format(
              "Inicializando sessão de credencial para requisitar a operação {0} no login {1}.",
              operation, remoteLogin));
        }

        CredentialData data = new CredentialData(BusId, login.id, sessionId,
          ticket, hash, chain);
        ServiceContext serviceContext =
          new ServiceContext(ContextId, _codec.encode_value(data));
        ri.add_request_service_context(serviceContext, false);
        Logger.Info(
          String.Format("Chamada à operação {0} no servidor de login {1}.",
            operation, remoteLogin));
      }
      catch (Exception) {
        Logger.Error(String.Format("Erro ao tentar enviar a requisição {0}.",
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

      switch (exception.Minor){
        case NoLoginCode.ConstVal:
        case UnavailableBusCode.ConstVal:
        case InvalidTargetCode.ConstVal:
        case InvalidRemoteCode.ConstVal:
          Logger.Error(
            String.Format(
              "Servidor chamado repassou uma exceção NO_PERMISSION local: minor = {0}.", exception.Minor));
          throw new NO_PERMISSION(InvalidRemoteCode.ConstVal, CompletionStatus.Completed_No);
        case InvalidLoginCode.ConstVal:
          Current current = Context.GetPICurrent();
          bool? noInvalidLoginHandling;
          try {
            noInvalidLoginHandling = (bool?)current.get_slot(_noInvalidLoginHandlingSlotId);
          }
          catch (InvalidSlot e) {
            Logger.Fatal(
              "Falha inesperada ao acessar o slot de tratamento de login inválido", e);
            throw;
          }
          // verifica se esta chamada deve tratar InvalidLogin
          if ((noInvalidLoginHandling == null) || (!noInvalidLoginHandling.Value)) {
            HandleInvalidLogin(ri);
          }
          break;
        case InvalidCredentialCode.ConstVal:
          HandleInvalidCredential(ri, exception);
          break;
      }
    }

    internal void ReceiveRequest(ServerRequestInfo ri,
      CredentialData credential) {
      string interceptedOperation = ri.operation;
      LoginInfo myLogin;
      AsymmetricKeyParameter busKey;
      String busId;
      _loginLock.EnterReadLock();
      try {
        if (!_login.HasValue) {
          Logger.Error(String.Format("Esta conexão está deslogada."));
          throw new NO_PERMISSION(UnknownBusCode.ConstVal,
            CompletionStatus.Completed_No);
        }
        myLogin = new LoginInfo(_login.Value.id, _login.Value.entity);
        busKey = _busKey;
        busId = _busId;
      }
      finally {
        _loginLock.ExitReadLock();
      }
      if (!credential.bus.Equals(busId)) {
        Logger.Error(String.Format(
          "A identificação do barramento está errada. O valor recebido foi '{0}' e o esperado era '{1}'.",
          credential.bus, busId));
        throw new NO_PERMISSION(UnknownBusCode.ConstVal,
          CompletionStatus.Completed_No);
      }

      string credentialLogin = credential.login;

      LoginCache.LoginEntry clientLogin;
      try {
        Context.SetCurrentConnection(this);
        clientLogin = _loginsCache.GetLoginEntry(credentialLogin);
      }
      catch (Exception e) {
        NO_PERMISSION noPermission = e as NO_PERMISSION;
        if (noPermission != null) {
          if (noPermission.Minor == NoLoginCode.ConstVal) {
            Logger.Error(
              "Este servidor foi deslogado do barramento durante a interceptação desta requisição.");
            throw new NO_PERMISSION(UnknownBusCode.ConstVal,
              CompletionStatus.Completed_No);
          }
        }
        Logger.Error("Não foi possível validar a credencial. Erro: " + e);
        throw new NO_PERMISSION(UnverifiedLoginCode.ConstVal,
          CompletionStatus.Completed_No);
      }

      if (clientLogin == null) {
        throw new NO_PERMISSION(InvalidLoginCode.ConstVal,
          CompletionStatus.Completed_No);
      }

      ServerSideSession session;
      if (_sessionId2Session.TryGetValue(credential.session, out session)) {
        // login tem que ser o mesmo que originou essa sessão
        if (credential.login.Equals(session.RemoteLogin)) {
          Logger.Debug(
            String.Format(
              "Testando hash com operação {0}, ticket {1} e segredo {2}, pertencente à sessão {3} do login {4}.",
              interceptedOperation, credential.ticket,
              BitConverter.ToString(session.Secret), session.Id,
              session.RemoteLogin));
          byte[] hash = CreateCredentialHash(interceptedOperation,
            credential.ticket,
            session.Secret);
          Logger.Debug("Hash recriado: " + BitConverter.ToString(hash));
          IStructuralEquatable eqHash = hash;
          if (eqHash.Equals(credential.hash,
            StructuralComparisons.StructuralEqualityComparer)) {
            // credencial valida
            // CheckChain pode lançar exceção com InvalidChainCode
            CallChain chain = CheckChain(credential.chain, credential.login,
              myLogin.entity, busId, busKey);
            // CheckTicket já faz o lock no ticket history da sessão
            if (session.CheckTicket(credential.ticket)) {
              // insere a cadeia no slot para a getCallerChain usar
              try {
                // já foi testado que o entity é o mesmo que o target, portanto posso inserir meu entity como target
                ri.set_slot(_chainSlotId,
                  new CallerChainImpl(chain.bus, chain.caller,
                    myLogin.entity,
                    chain.originators,
                    credential.chain));
              }
              catch (InvalidSlot e) {
                Logger.Fatal(
                  "Falha ao inserir o identificador de login em seu slot.", e);
                throw;
              }
              return;
            }
            Logger.Debug(String.Format("O ticket {0} não confere.",
              credential.ticket));
          }
          else {
            Logger.Debug("O hash não confere com o esperado.");
          }
        }
        else {
          Logger.Debug(
            String.Format(
              "O login associado a esta sessão é {0} e a credencial foi enviada com o login {1}.",
              session.RemoteLogin, credential.login));
        }
      }
      else {
        Logger.Debug(
          String.Format("Não foi encontrada a sessão {0} nesta conexão.",
            credential.session));
      }

      // credencial invalida por nao ter sessao conhecida, ticket inválido, hash errado ou login errado
      Logger.Debug("Credencial inválida, enviando CredentialReset.");
      // FIXME
      // Uma explicação detalhada para a linha abaixo encontra-se em um FIXME 
      // no código do interceptador servidor, no método receive_request.
      ri.set_slot(_chainSlotId, "reset");
    }

    internal void SendException(ServerRequestInfo ri,
      CredentialData credential) {
      // CreateCredentialReset pode lançar exceção com UnverifiedLoginCode e InvalidPublicKeyCode
      byte[] encodedReset =
        CreateCredentialReset(credential.login);
      ServiceContext replyServiceContext = new ServiceContext(ContextId,
        encodedReset);
      ri.add_reply_service_context(replyServiceContext, false);
    }

    private LoginInfo? GetLogin(LoginInfo? originalLogin) {
      LoginInfo? login = null;
      LoginInfo? invalidLogin = null;
      InvalidLoginCallback onInvalidLogin;
      LoginInfo originalCopy = originalLogin.HasValue
        ? new LoginInfo(originalLogin.Value.id,
          originalLogin.Value.entity)
        : new LoginInfo();
      _loginLock.EnterReadLock();
      try {
        if (_login.HasValue) {
          login = new LoginInfo(_login.Value.id, _login.Value.entity);
          if (!originalLogin.HasValue) {
            originalCopy.id = _login.Value.id;
            originalCopy.entity = _login.Value.entity;
          }
        }
        if (_invalidLogin.HasValue) {
          invalidLogin = new LoginInfo(_invalidLogin.Value.id,
            _invalidLogin.Value.entity);
        }
        onInvalidLogin = _onInvalidLoginCallback;
      }
      finally {
        _loginLock.ExitReadLock();
      }
      if (!login.HasValue) {
        if (!invalidLogin.HasValue) {
          return null;
        }
        // tenta se recuperar de um login inválido
        LoginInfo? originalInvalid = new LoginInfo(invalidLogin.Value.id,
          invalidLogin.Value.entity);
        while (originalInvalid.HasValue) {
          if (onInvalidLogin != null) {
            try {
              onInvalidLogin(this, originalCopy);
            }
            catch (Exception e) {
              Logger.Warn("Callback OnInvalidLogin lançou exceção: ", e);
            }
            _loginLock.EnterWriteLock();
            try {
// ReSharper disable ConditionIsAlwaysTrueOrFalse
              if (!_invalidLogin.HasValue ||
// ReSharper restore ConditionIsAlwaysTrueOrFalse
                  _invalidLogin.Value.id.Equals(originalInvalid.Value.id)) {
                _invalidLogin = null;
                originalInvalid = null;
              }
              else {
                originalInvalid = new LoginInfo(_invalidLogin.Value.id,
                  _invalidLogin.Value.entity);
              }
            }
            finally {
              _loginLock.ExitWriteLock();
            }
          }
          else {
            break;
          }
        }
      }
      _loginLock.EnterReadLock();
      try {
        if (_login.HasValue) {
          return new LoginInfo(_login.Value.id, _login.Value.entity);
        }
        return null;
      }
      finally {
        _loginLock.ExitReadLock();
      }
    }

    private void HandleInvalidLogin(ClientRequestInfo ri){
      String operation = ri.operation;
      Current current = Context.GetPICurrent();
      LoginInfo originalLogin;
      try {
        originalLogin = (LoginInfo)current.get_slot(_loginSlotId);
      }
      catch (InvalidSlot e) {
        Logger.Fatal(
          "Falha inesperada ao acessar o slot do login corrente", e);
        throw;
      }
      Logger.Error(
        String.Format(
          "Exceção de login inválido recebida ao tentar realizar a operação {0} com o login {1} da entidade {2}.",
          operation, originalLogin.id, originalLogin.entity));
      Logger.Debug(
        String.Format(
          "O login recuperado do PICurrent tem entidade {0} e id {1}.",
          originalLogin.entity, originalLogin.id));

      // se o login eh o mesmo que o anterior, verifica se esta realmente invalido
      LoginRegistry lr;
      LoginInfo? actualLogin;
      _loginLock.EnterReadLock();
      try{
        lr = _loginRegistry;
        actualLogin = _login;
      }
      finally{
        _loginLock.ExitReadLock();
      }
      if (actualLogin.HasValue &&
          originalLogin.id.Equals(actualLogin.Value.id)) {
        // verifica se o login está realmente inválido
        try {
          bool? previousSlotValue = (bool?)current.get_slot(_noInvalidLoginHandlingSlotId);
          current.set_slot(_noInvalidLoginHandlingSlotId, true);
          // para a chamada getLoginValidity não queremos tratar um eventual
          // InvalidLogin com relogin, queremos só receber a exceção.
          try {
            if (lr.getLoginValidity(originalLogin.id) > 0) {
              Logger.Error(
                String.Format(
                  "Servidor remoto indicou uma condição InvalidLogin falsa. Login:{0}. Entidade:{1}.",
                  originalLogin.id, originalLogin.entity));
              throw new NO_PERMISSION(InvalidRemoteCode.ConstVal,
                CompletionStatus.Completed_No);
            }
            // login anterior era inválido mas login atual é válido pois não gerou exceção
            // vai retentar a operação e fazer logout se necessário
          }
          catch (NO_PERMISSION e) {
            // como a chamada getLoginValidity é feita sem handling de InvalidLogin, caso o erro ocorra chegará aqui
            switch (e.Minor) {
              case InvalidRemoteCode.ConstVal:
                throw;
              case InvalidLoginCode.ConstVal:
                // significa que meu login está realmente inválido já que foi uma chamada ao barramento
                // vai retentar a operação e fazer logout se necessário
                break;
              default:
                Logger.Error(
                  String.Format(
                    "Não foi possível verificar a validade do login. Login:{0}. Entidade:{1}.",
                    originalLogin.id, originalLogin.entity));
                throw new NO_PERMISSION(UnavailableBusCode.ConstVal,
                  CompletionStatus.Completed_No);
            }
          }
          finally {
            current.set_slot(_noInvalidLoginHandlingSlotId, previousSlotValue);
          }
        }
        catch (InvalidSlot e) {
          Logger.Fatal(
            "Falha inesperada ao acessar o slot de tratamento de login inválido", e);
          throw;
        }
      }
      else {
        // senao, tenta reobter o login e possivelmente refazer a operacao
        string loginId = actualLogin.HasValue ? actualLogin.Value.id : "null";
        Logger.Debug(String.Format(
          "Login inválido é diferente do que tentou realizar a operação. Uma nova tentativa será feita. Login anterior: {0}. Login atual: {1}.", originalLogin.id, loginId));
      }
      // verifica se é necessário o logout e refaz a operação
      CheckLoginAndTryRelogin(ri, originalLogin);
    }

    private void CheckLoginAndTryRelogin(ClientRequestInfo ri, LoginInfo originalLogin) {
      String operation = ri.operation;
      _loginLock.EnterWriteLock();
      try {
        // se login mudou no meio tempo, tenta novamente sem fazer logout
        if (_login.HasValue &&
            originalLogin.id.Equals(_login.Value.id)) {
          Logger.Debug(
            "Login inválido ainda é o mesmo que tentou realizar a operação.");
          LocalLogout();
          _invalidLogin = originalLogin;
        }
        else {
          Logger.Debug(
            "Login inválido é diferente do que tentou realizar a operação.");
        }
      }
      finally {
        _loginLock.ExitWriteLock();
      }
      LoginInfo? login =
        GetLoginOrThrowNoLogin(
          String.Format(
            "Login não foi reestabelecido, impossível realizar a operação {0}.",
            operation), originalLogin);
      // tenta refazer o request
      Logger.Debug("Login reestabelecido.");
      Logger.Info(
        String.Format(
          "Tentativa de refazer a operação {0} com o login {1} da entidade {2}.",
          operation, login.Value.id, login.Value.entity));
      throw new ForwardRequest(ri.target);
    }

    private void HandleInvalidCredential(ClientRequestInfo ri, NO_PERMISSION exception) {
      CredentialReset requestReset = ReadCredentialReset(ri, exception);
      string remoteLogin = requestReset.target;
      EffectiveProfile profile = new EffectiveProfile(ri.effective_profile);
      _profile2Login.TryAdd(profile, remoteLogin);

      int sessionId = requestReset.session;
      byte[] secret;
      try {
        secret = Crypto.Decrypt(_internalKeyPair.Private, requestReset.challenge);
      }
      catch (Exception) {
        throw new NO_PERMISSION(InvalidRemoteCode.ConstVal, CompletionStatus.Completed_No);
      }
      _outgoingLogin2Session.TryAdd(remoteLogin,
                                    new ClientSideSession(sessionId, secret,
                                                          remoteLogin));
      Logger.Debug(
        String.Format(
          "Início de sessão de credencial {0} ao tentar requisitar a operação {1} ao login {2}.",
          sessionId, ri.operation, remoteLogin));
      // pede que a chamada original seja relançada
      throw new ForwardRequest(ri.target);
    }

    private LoginInfo GetLoginOrThrowNoLogin(string errorMsg,
      LoginInfo? originalLogin) {
      LoginInfo? login = GetLogin(originalLogin);
      if (!login.HasValue) {
        Logger.Error(errorMsg);
        throw new NO_PERMISSION(NoLoginCode.ConstVal,
          CompletionStatus.Completed_No);
      }
      return login.Value;
    }

    private SignedData CreateCredentialSignedCallChain(string remoteLogin) {
      SignedData signed;
      CallerChainImpl chain = Context.JoinedChain as CallerChainImpl;
      Logger.Debug(
        string.Format("Requisição para o login {0} tem joined chain? {1}.",
          remoteLogin, chain != null));
      if (!remoteLogin.Equals(BusLogin.ConstVal)) {
        // esta requisição não é para o barramento, então preciso assinar essa cadeia.
        if (chain == null) {
          // na chamada a signChainFor vai criar uma nova chain e assinar
          SignCallChain(remoteLogin, out signed);
        }
        else {
          bool cacheHit;
          lock (chain) {
            cacheHit = chain.Joined.TryGetValue(remoteLogin, out signed);
          }
          if (!cacheHit) {
            SignCallChain(remoteLogin, out signed);
            lock (chain) {
              chain.Joined.TryAdd(remoteLogin, signed);
            }
          }
        }
      }
      else {
        // requisição para o barramento
        if (chain == null) {
          return _invalidSignedData;
        }
        signed = chain.Signed;
      }
      return signed;
    }

    private void SignCallChain(string remoteLogin, out SignedData signed) {
      // se o login mudar, tem que assinar de novo
      while (true) {
        AccessControl localAcs;
        string busId;
        _loginLock.EnterReadLock();
        try {
          localAcs = _acs;
          busId = _busId;
        }
        finally {
          _loginLock.ExitReadLock();
        }
        try {
          signed = localAcs.signChainFor(remoteLogin);
        }
        catch (AbstractCORBASystemException e) {
          Logger.Error("Erro ao acessar o barramento " + busId + ".", e);
          throw new NO_PERMISSION(UnavailableBusCode.ConstVal,
            CompletionStatus.Completed_No);
        }
        catch (InvalidLogins e) {
          Logger.Error("Chamada a um serviço com um login inválido.", e);
          throw new NO_PERMISSION(InvalidTargetCode.ConstVal,
            CompletionStatus.Completed_No);
        }
        LoginInfo actualLogin =
          GetLoginOrThrowNoLogin(
            "Impossível gerar cadeia para a chamada, pois o login foi perdido.",
            null);
        CallChain newChain = Context.UnmarshalCallChain(signed);
        if (actualLogin.id.Equals(newChain.caller.id)) {
          break;
        }
      }
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
        Logger.Error(
          "Erro na tentativa de extrair a informação de reset.", e);
        throw new NO_PERMISSION(InvalidRemoteCode.ConstVal, exception.Status);
      }
      return requestReset;
    }

    private byte[] CreateCredentialReset(string remoteLogin) {
      string loginId;
      _loginLock.EnterReadLock();
      try {
        if (!_login.HasValue) {
          // Este servidor não está logado no barramento
          Logger.Error(
            "Este servidor não está logado no barramento e portanto não pode criar um CredentialReset.");
          throw new NO_PERMISSION(UnknownBusCode.ConstVal,
            CompletionStatus.Completed_No);
        }
        loginId = _login.Value.id;
      }
      finally {
        _loginLock.ExitReadLock();
      }
      CredentialReset reset = new CredentialReset {target = loginId};
      byte[] challenge = new byte[SecretSize];
      Random rand = new Random();
      rand.NextBytes(challenge);

      LoginCache.LoginEntry login;
      try {
        Context.SetCurrentConnection(this);
        login = _loginsCache.GetLoginEntry(remoteLogin);
        if (login == null) {
          Logger.Error(
            "Não foi encontrada uma entrada na cache de logins para o login " +
            remoteLogin);
          throw new NO_PERMISSION(UnverifiedLoginCode.ConstVal,
            CompletionStatus.Completed_No);
        }
      }
      catch (Exception e) {
        NO_PERMISSION noPermission = e as NO_PERMISSION;
        if (noPermission != null) {
          if (noPermission.Minor == NoLoginCode.ConstVal) {
            Logger.Error(
              "Este servidor foi deslogado do barramento durante a interceptação desta requisição.");
            throw new NO_PERMISSION(UnknownBusCode.ConstVal,
              CompletionStatus.Completed_No);
          }
        }
        Logger.Error(
          String.Format("Não foi possível validar o login {0}. Erro: {1}",
            remoteLogin, e));
        throw new NO_PERMISSION(UnverifiedLoginCode.ConstVal,
          CompletionStatus.Completed_No);
      }

      try {
        reset.challenge = Crypto.Encrypt(login.Publickey, challenge);
      }
      catch (Exception) {
        throw new NO_PERMISSION(InvalidPublicKeyCode.ConstVal,
          CompletionStatus.Completed_No);
      }

      // lock para tornar esse trecho atomico
      int sessionId;
      _sessionIdLock.EnterWriteLock();
      try {
        sessionId = _sessionId;
        _sessionId++;
      }
      finally {
        _sessionIdLock.ExitWriteLock();
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

    private CallChain CheckChain(SignedData signed, string callerId,
      string entity, string busId, AsymmetricKeyParameter busKey) {
      CallChain chain = Context.UnmarshalCallChain(signed);
      if (!chain.target.Equals(entity)) {
        Logger.Error(
          "O entity não é o mesmo do alvo da cadeia. É necessário refazer a sessão de credencial através de um reset.");
        throw new NO_PERMISSION(InvalidCredentialCode.ConstVal,
          CompletionStatus.Completed_No);
      }
      if (!chain.caller.id.Equals(callerId) || (!chain.bus.Equals(busId)) ||
          (!Crypto.VerifySignature(busKey, signed.encoded, signed.signature))) {
        Logger.Error("Cadeia de credencial inválida.");
        throw new NO_PERMISSION(InvalidChainCode.ConstVal,
          CompletionStatus.Completed_No);
      }
      return chain;
    }

    private byte[] CreateCredentialHash(string operation, int ticket,
      byte[] secret) {
      Encoding enc = Crypto.TextEncoding;
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