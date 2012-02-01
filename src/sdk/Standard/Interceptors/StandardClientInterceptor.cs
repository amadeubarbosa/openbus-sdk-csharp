using System;
using System.Security.Cryptography;
using System.Text;
using Ch.Elca.Iiop.Idl;
using log4net;
using omg.org.CORBA;
using omg.org.IOP;
using omg.org.PortableInterceptor;
using tecgraf.openbus.core.v2_00.credential;
using tecgraf.openbus.core.v2_00.services.access_control;
using tecgraf.openbus.sdk.Exceptions;
using tecgraf.openbus.sdk.Interceptors;
using tecgraf.openbus.sdk.Security;
using System.Collections.Generic;

namespace tecgraf.openbus.sdk.Standard.Interceptors {
  /// <summary>
  /// Representa o interceptador cliente.
  /// Implementa PortableInterceptor.ClientRequestInterceptor.
  /// </summary>
  internal class StandardClientInterceptor : InterceptorImpl,
                                             ClientRequestInterceptor {
    #region Fields

    private static readonly ILog Logger =
      LogManager.GetLogger(typeof (StandardClientInterceptor));

    private readonly StandardOpenbus _bus;
    private readonly StandardConnection _connection;

    //TODO: avaliar a melhor forma de armazenar a chave. String é uma boa opção? Se fosse usar o array de bytes direto eu teria q criar uma classe com metodos de comparacao que criasse um hash, para nao ficar muito cara a comparacao...
    private readonly Dictionary<String, string> _profile2Login;

    private readonly Dictionary<string, Session> _outgoingLogin2Session;

    #endregion

    #region Contructor

    /// <summary>
    /// Inicializa uma nova instância de OpenbusAPI.Interceptors.StandardClientInterceptor
    /// </summary>
    /// <param name="codec">Codificador.</param>
    /// <param name="bus">Barramento de uma única conexão.</param>
    public StandardClientInterceptor(StandardOpenbus bus, Codec codec)
      : base("StandardClientInterceptor", codec) {
      _bus = bus;
      _connection = _bus.Connect() as StandardConnection;
      _profile2Login = new Dictionary<String, string>();
      _outgoingLogin2Session = new Dictionary<String, Session>();
    }

    #endregion

    #region ClientRequestInterceptor Members

    /// <summary>
    /// Intercepta o request para inserção de informação de contexto.
    /// </summary>
    /// <remarks>Informação do cliente</remarks>
    public void send_request(ClientRequestInfo ri) {
      string operation = ri.operation;
      Logger.Debug(
        String.Format(
          "Interceptador cliente iniciando tentativa de chamada à operação {0}.",
          operation));

      LoginInfo? login = _connection.Login;
      if (!login.HasValue) {
        Logger.Debug(
          String.Format(
            "Chamada à operação {0} cancelada devido a não existir login.",
            operation));
        throw new NO_PERMISSION(NoLoginCode.ConstVal,
                                CompletionStatus.Completed_No);
      }
      string loginId = login.Value.id;
      int sessionId = 0;
      int ticket = 0;
      byte[] secret = new byte[SecretSize];
      byte[] hash;
      SignedCallChain chain;

      string profile = ri.effective_profile.tag +
                       ri.effective_profile.profile_data.ToString();
      string remoteLogin = "";
      // Uso o bool abaixo para nao precisar aninhar os locks.
      bool hasSession = false;
      lock (_profile2Login) {
        if (_profile2Login.ContainsKey(profile)) {
          remoteLogin = _profile2Login[profile];
        }
      }
      lock (_outgoingLogin2Session) {
        if (_outgoingLogin2Session.ContainsKey(remoteLogin)) {
          Session session = _outgoingLogin2Session[remoteLogin];
          sessionId = session.Id;
          ticket = session.Ticket;
          session.Secret.CopyTo(secret, 0);
          hasSession = true;
        }
      }

      if (hasSession) {
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

      byte[] value;
      try {
        value = CreateAndEncodeCredential(loginId, sessionId, ticket, hash,
                                          chain);
      }
      catch (Exception) {
        Logger.Fatal("Erro ao tentar codificar a credencial.");
        throw;
      }
      ServiceContext serviceContext = new ServiceContext(ContextId, value);
      ri.add_request_service_context(serviceContext, false);
    }

    #endregion

    #region ClientRequestInterceptor Not Implemented

    /// <inheritdoc />
    public virtual void receive_exception(ClientRequestInfo ri) {
      String operation = ri.operation;
      String exceptionId = ri.received_exception_id;
      Logger.Info(String.Format(
        "A exceção '{0}' foi interceptada ao tentar realizar a chamada {1}.",
        exceptionId, operation));

      if (!(ri.received_exception is NO_PERMISSION)) {
        return;
      }
      NO_PERMISSION exception = ri.received_exception as NO_PERMISSION;

      if (exception.Minor != InvalidCredentialCode.ConstVal) {
        return;
      }

      CredentialReset requestReset = ReadCredentialReset(ri, exception);
      string remoteLogin = requestReset.login;
      string profile = ri.effective_profile.tag +
                       ri.effective_profile.profile_data.ToString();
      lock (_profile2Login) {
        _profile2Login.Add(profile, remoteLogin);
      }

      lock (_outgoingLogin2Session) {
        if (_outgoingLogin2Session.ContainsKey(remoteLogin)) {
          Logger.Info(
            String.Format(
              "Reuso de sessão de credencial {0} ao tentar requisitar a operação {1} ao login {2}.",
              _outgoingLogin2Session[remoteLogin], operation, remoteLogin));
        }
        else {
          int session = requestReset.session;
          byte[] secret = Crypto.Decrypt(_connection.PrivateKey,
                                         requestReset.challenge);
          _outgoingLogin2Session.Add(remoteLogin,
                                     new Session(session, secret,
                                                 remoteLogin));
          Logger.Info(
            String.Format(
              "Início de sessão de credencial {0} ao tentar requisitar a operação {1} ao login {2}.",
              session, operation, remoteLogin));
        }
      }
      // pede que a chamada original seja relançada
      throw new ForwardRequest(ri.target);
    }

    /// <inheritdoc />
    public virtual void receive_other(ClientRequestInfo ri) {
      //Nada a ser feito;
    }

    /// <inheritdoc />
    public virtual void receive_reply(ClientRequestInfo ri) {
      //Nada a ser feito;
    }

    /// <inheritdoc />
    public virtual void send_poll(ClientRequestInfo ri) {
      //Nada a ser feito;
    }

    #endregion

    private byte[] CreateCredentialHash(string operation, int ticket,
                                        byte[] secret, int requestId) {
      UTF8Encoding utf8 = new UTF8Encoding();
      // 2 bytes para versao, 16 para o segredo, 4 para o ticket em little endian, 4 para o request id em little endian e operacao.
      byte[] hash = new byte[26 + utf8.GetByteCount(operation)];
      hash[0] = MajorVersion;
      hash[1] = MinorVersion;
      secret.CopyTo(hash, 2);
      byte[] bTicket = BitConverter.GetBytes(ticket);
      byte[] bRequestId = BitConverter.GetBytes(requestId);
      if (!BitConverter.IsLittleEndian) {
        Array.Reverse(bTicket);
        Array.Reverse(bRequestId);
      }
      bTicket.CopyTo(hash, 18);
      bRequestId.CopyTo(hash, 22);
      byte[] bOperation = utf8.GetBytes(operation);
      bOperation.CopyTo(hash, 26);
      return SHA256.Create().ComputeHash(hash);
    }

    private byte[] CreateInvalidCredentialHash() {
      return new byte[] {0};
    }

    private SignedCallChain CreateCredentialSignedCallChain(string remoteLogin) {
      //TODO: se for o barramento, retornar inválida ou "nula"?
      return !remoteLogin.Equals(_bus.BusId)
               ? _bus.Acs.signChainFor(remoteLogin)
               : CreateInvalidCredentialSignedCallChain();
    }

    private SignedCallChain CreateInvalidCredentialSignedCallChain() {
      return new SignedCallChain(new byte[] {0}, new byte[0]);
    }

    private byte[] CreateAndEncodeCredential(string loginId, int sessionId,
                                             int ticket, byte[] hash,
                                             SignedCallChain chain) {

      CredentialData data = new CredentialData(_bus.BusId, loginId, sessionId,
                                               ticket,
                                               hash, chain);
      byte[] value = Codec.encode_value(data);
      byte[] valueComVersao = new byte[value.Length + 2];
      valueComVersao[0] = MajorVersion;
      valueComVersao[1] = MinorVersion;
      value.CopyTo(valueComVersao, 2);
      return valueComVersao;
    }

    private CredentialReset ReadCredentialReset(ClientRequestInfo ri, NO_PERMISSION exception) {
      CredentialReset requestReset;

      try {
        ServiceContext serviceContext =
          ri.get_request_service_context(ContextId);

        OrbServices orb = OrbServices.GetSingleton();
        Type resetType = typeof(CredentialReset);
        omg.org.CORBA.TypeCode resetTypeCode =
          orb.create_interface_tc(
            Repository.GetRepositoryID(resetType), resetType.Name);

        byte[] data = serviceContext.context_data;
        if ((data[0] != MajorVersion) || (data[1] != MinorVersion)) {
          Logger.Fatal(String.Format(
            "Credencial recebida para início de sessão não é da versão {0}.{1}. Sessão não será iniciada.",
            MajorVersion, MinorVersion),
                       exception);
          throw new IncompatibleVersionException(String.Format("A versão do objeto remoto é {0}.{1}.", data[0], data[1]));
        }
        byte[] resetData = new byte[data.Length - 2];
        for (int i = 2; i < data.Length; i++) {
          resetData[i - 2] = data[i];
        }

        requestReset =
          (CredentialReset)Codec.decode_value(resetData, resetTypeCode);
      }
      catch (Exception e) {
        Logger.Fatal(
          "Erro na tentativa de extrair a informação de reset.", e);
        throw new NO_PERMISSION(InvalidRemoteCode.ConstVal, exception.Status);
      }
      return requestReset;
    }
  }
}