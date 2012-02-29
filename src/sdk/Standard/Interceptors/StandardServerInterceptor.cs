using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Ch.Elca.Iiop.Idl;
using log4net;
using omg.org.CORBA;
using omg.org.IOP;
using omg.org.PortableInterceptor;
using tecgraf.openbus.core.v2_00.credential;
using tecgraf.openbus.core.v2_00.services.access_control;
using tecgraf.openbus.sdk.Interceptors;
using tecgraf.openbus.sdk.Security;

namespace tecgraf.openbus.sdk.Standard.Interceptors {
  internal class StandardServerInterceptor : InterceptorImpl,
                                             ServerRequestInterceptor {
    #region Fields

    private static readonly ILog Logger =
      LogManager.GetLogger(typeof (StandardServerInterceptor));

    private readonly StandardOpenbus _bus;
    private readonly StandardConnection _connection;

    //TODO: caches ainda nao tem nenhuma politica de remocao ou de tamanho maximo
    private int _sessionId = 1;
    private readonly Dictionary<int, Session> _sessionId2Session;
    private readonly Dictionary<string, RSACryptoServiceProvider> _login2PubKey;
    private readonly object _lock = new object();

    #endregion

    #region Constructors

    /// <summary>
    /// Inicializa uma nova instância de OpenbusAPI.Interceptors.StandardServerInterceptor.   
    /// </summary>
    /// <param name="bus">Barramento de uma única conexão.</param>
    /// <param name="codec">Codificador.</param>
    public StandardServerInterceptor(StandardOpenbus bus, Codec codec)
      : base("StandardServerInterceptor", codec) {
      _bus = bus;
      _connection = _bus.Connect() as StandardConnection;
      _sessionId2Session = new Dictionary<int, Session>();
      _login2PubKey = new Dictionary<string, RSACryptoServiceProvider>();
    }

    #endregion

    #region ServerRequestInterceptor Implemented

    /// <inheritdoc />
    public void receive_request(ServerRequestInfo ri) {
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
      if (!credential.bus.Equals(_bus.BusId)) {
        Logger.Fatal(
          String.Format(
            "A identificação do barramento está errada. O valor recebido foi '{0}' e o esperado era '{1}'.",
            _bus.BusId, credential.bus));
        throw new NO_PERMISSION(UnknownBusCode.ConstVal,
                                CompletionStatus.Completed_No);
      }

      // CheckValidity lança exceção caso a validade tenha expirado
      CheckValidity(credential);

      byte[] secret = new byte[0];
      int ticket = 0;
      lock (_sessionId2Session) {
        if (_sessionId2Session.ContainsKey(credential.session)) {
          Session session = _sessionId2Session[credential.session];
          secret = session.Secret;
          ticket = session.Ticket;
        }
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

    private byte[] CreateCredentialReset(string remoteLogin) {
      if (_connection.Login != null) {
        CredentialReset reset = new CredentialReset();
        reset.login = _connection.Login.Value.id;
        reset.challenge = new byte[SecretSize];
        Random rand = new Random();
        rand.NextBytes(reset.challenge);
        RSACryptoServiceProvider pubKey;

        // lock para tornar esse trecho atomico
        lock (_lock) {
          lock (_login2PubKey) {
            if (!_login2PubKey.TryGetValue(remoteLogin, out pubKey)) {
              byte[] key;
              try {
                _bus.LoginRegistry.getLoginInfo(remoteLogin, out key);
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
              _login2PubKey.Add(remoteLogin, pubKey);
            }
          }
          reset.challenge = Crypto.Encrypt(pubKey, reset.challenge);

          lock (_sessionId2Session) {
            reset.session = _sessionId++;
            Session session = new Session(reset.session, reset.challenge,
                                          remoteLogin);
            _sessionId2Session.Add(reset.session, session);
          }
        }
        return Codec.encode_value(reset);
      }
      // Este servidor não está logado no barramento
      Logger.Fatal("Este servidor não está logado no barramento");
      throw new NO_PERMISSION(UnverifiedLoginCode.ConstVal,
                              CompletionStatus.Completed_No);
    }

    private void CheckValidity(CredentialData credential) {
      int validity;
      try {
        validity = _bus.LoginRegistry.getValidity(new[] {credential.login})[0];
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

    #endregion

    #region ServerRequestInterceptor Not Implemented

    /// <inheritdoc />
    public virtual void receive_request_service_contexts(ServerRequestInfo ri) {
      //Nada a ser feito
    }

    /// <inheritdoc />
    public virtual void send_exception(ServerRequestInfo ri) {
      //Nada a ser feito
    }

    /// <inheritdoc />
    public virtual void send_other(ServerRequestInfo ri) {
      //Nada a ser feito
    }

    /// <inheritdoc />
    public virtual void send_reply(ServerRequestInfo ri) {
      //Nada a ser feito
    }

    #endregion

    private CredentialData UnmarshalCredential(ServiceContext serviceContext) {
      OrbServices orb = OrbServices.GetSingleton();
      Type credentialType = typeof (CredentialData);
      omg.org.CORBA.TypeCode credentialTypeCode =
        orb.create_interface_tc(Repository.GetRepositoryID(credentialType),
                                credentialType.Name);

      byte[] data = serviceContext.context_data;
      return (CredentialData) Codec.decode_value(data, credentialTypeCode);
    }

    private void CheckChain(SignedCallChain signed, string callerId) {
      CallChain chain = UnmarshalCallChain(signed);
      try {
        if (!chain.target.Equals(_connection.Login.Value) ||
            (!chain.callers[chain.callers.Length - 1].id.Equals(callerId)) ||
            (!_bus.BusKey.VerifyData(signed.encoded, SHA256.Create(), signed.signature))) {
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

    private CallChain UnmarshalCallChain(SignedCallChain signed) {
      OrbServices orb = OrbServices.GetSingleton();
      Type chainType = typeof (CallChain);
      omg.org.CORBA.TypeCode chainTypeCode =
        orb.create_interface_tc(Repository.GetRepositoryID(chainType),
                                chainType.Name);
      return (CallChain) Codec.decode_value(signed.encoded, chainTypeCode);
    }
                                             }
}