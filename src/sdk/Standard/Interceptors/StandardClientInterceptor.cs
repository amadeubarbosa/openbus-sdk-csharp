using System;
using System.Collections.Generic;
using Ch.Elca.Iiop.Idl;
using log4net;
using omg.org.CORBA;
using omg.org.IOP;
using omg.org.PortableInterceptor;
using tecgraf.openbus.core.v2_00.credential;
using tecgraf.openbus.core.v2_00.services.access_control;
using tecgraf.openbus.sdk.Interceptors;
using tecgraf.openbus.sdk.Security;
using TypeCode = omg.org.CORBA.TypeCode;

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

    //TODO: avaliar a melhor forma de armazenar a chave. String � uma boa op��o? Se fosse usar o array de bytes direto eu teria q criar uma classe com metodos de comparacao que criasse um hash, para nao ficar muito cara a comparacao...

    //TODO: caches ainda nao tem nenhuma politica de remocao ou de tamanho maximo
    private readonly Dictionary<string, Session> _outgoingLogin2Session;
    private readonly Dictionary<String, string> _profile2Login;

    #endregion

    #region Contructor

    /// <summary>
    /// Inicializa uma nova inst�ncia de OpenbusAPI.Interceptors.StandardClientInterceptor
    /// </summary>
    /// <param name="codec">Codificador.</param>
    /// <param name="bus">Barramento de uma �nica conex�o.</param>
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
    /// Intercepta o request para inser��o de informa��o de contexto.
    /// </summary>
    /// <remarks>Informa��o do cliente</remarks>
    public void send_request(ClientRequestInfo ri) {
      string operation = ri.operation;
      Logger.Debug(
        String.Format(
          "Interceptador cliente iniciando tentativa de chamada � opera��o {0}.",
          operation));

      LoginInfo? login = _connection.Login;
      if (!login.HasValue) {
        Logger.Debug(
          String.Format(
            "Chamada � opera��o {0} cancelada devido a n�o existir login.",
            operation));
        throw new NO_PERMISSION(NoLoginCode.ConstVal,
                                CompletionStatus.Completed_No);
      }
      string loginId = login.Value.id;
      int sessionId = 0;
      int ticket = 0;
      byte[] secret = new byte[0];

      string profile = ri.effective_profile.tag +
                       ri.effective_profile.profile_data.ToString();
      string remoteLogin = "";
      // Uso o bool abaixo para nao precisar aninhar os locks.
      lock (_profile2Login) {
        if (_profile2Login.ContainsKey(profile)) {
          remoteLogin = _profile2Login[profile];
        }
      }
      lock (_outgoingLogin2Session) {
        if (_outgoingLogin2Session.ContainsKey(remoteLogin)) {
          Session session = _outgoingLogin2Session[remoteLogin];
          sessionId = session.Id;
          ticket = session.Ticket + 1;
          secret = new byte[session.Secret.Length];
          session.Secret.CopyTo(secret, 0);
        }
      }

      try {
        byte[] hash;
        SignedCallChain chain;
        if (sessionId != 0) {
          hash = CreateCredentialHash(operation, ticket, secret, ri.request_id);
          //TODO: codigo abaixo assume que nao existe cache de cadeias ainda
          chain = CreateCredentialSignedCallChain(remoteLogin);
          Logger.Info(
            String.Format("Chamada � opera��o {0} no servidor de login {1}.",
                          operation, remoteLogin));
        }
        else {
          // Cria credencial inv�lida para iniciar o handshake e obter uma nova sess�o
          hash = CreateInvalidCredentialHash();
          chain = CreateInvalidCredentialSignedCallChain();
          Logger.Info(
            String.Format(
              "Inicializando sess�o de credencial para requisitar a opera��o {0} no login {1}.",
              operation, remoteLogin));
        }

        byte[] value = CreateAndEncodeCredential(loginId, sessionId, ticket, hash,
                                                 chain);
        ServiceContext serviceContext = new ServiceContext(ContextId, value);
        ri.add_request_service_context(serviceContext, false);
      }
      catch (NO_PERMISSION e) {
        if (e.Minor == InvalidLoginCode.ConstVal) {
          Logger.Fatal(
            "Este cliente foi deslogado do barramento durante a intercepta��o desta requisi��o.",
            e);
          //TODO chamar callback de login perdido
          //TODO se callback retornar true, relan�ar o request
          // se callback retornar false, tentativas de refazer login falharam, lan�a exce��o
          throw new NO_PERMISSION(InvalidLoginCode.ConstVal,
                                  CompletionStatus.Completed_No);
        }
        throw;
      }
      catch (Exception) {
        Logger.Fatal(String.Format("Erro ao tentar enviar a requisi��o {0}.", operation));
        throw;
      }
    }

    /// <inheritdoc />
    public virtual void receive_exception(ClientRequestInfo ri) {
      String operation = ri.operation;
      String exceptionId = ri.received_exception_id;
      Logger.Info(String.Format(
        "A exce��o '{0}' foi interceptada ao tentar realizar a chamada {1}.",
        exceptionId, operation));

      if (!(ri.received_exception_id.Equals(Repository.GetRepositoryID(typeof(NO_PERMISSION))))) {
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
      lock (_profile2Login) {
        _profile2Login.Add(profile, remoteLogin);
      }

      lock (_outgoingLogin2Session) {
        if (_outgoingLogin2Session.ContainsKey(remoteLogin)) {
          Logger.Info(
            String.Format(
              "Reuso de sess�o de credencial {0} ao tentar requisitar a opera��o {1} ao login {2}.",
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
              "In�cio de sess�o de credencial {0} ao tentar requisitar a opera��o {1} ao login {2}.",
              session, operation, remoteLogin));
        }
      }
      // pede que a chamada original seja relan�ada
      throw new ForwardRequest(ri.target);
    }

    #endregion

    #region ClientRequestInterceptor Not Implemented

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

    #region Private Methods

    private byte[] CreateInvalidCredentialHash() {
      return new byte[] {0};
    }

    private SignedCallChain CreateCredentialSignedCallChain(string remoteLogin) {
      //TODO: se for o barramento, retornar inv�lida ou "nula"?
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
          (CredentialReset) Codec.decode_value(data, resetTypeCode);
      }
      catch (Exception e) {
        Logger.Fatal(
          "Erro na tentativa de extrair a informa��o de reset.", e);
        throw new NO_PERMISSION(InvalidRemoteCode.ConstVal, exception.Status);
      }
      return requestReset;
    }

  #endregion
  }
}