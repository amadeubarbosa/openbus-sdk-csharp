using System;
using System.Text;
using Ch.Elca.Iiop.Util;
using log4net;
using omg.org.CORBA;
using omg.org.IOP;
using omg.org.PortableInterceptor;
using tecgraf.openbus.core.v1_05.access_control_service;
using tecgraf.openbus.core.v2_0.credential;
using tecgraf.openbus.core.v2_0.services.access_control;
using TypeCode = omg.org.CORBA.TypeCode;

namespace tecgraf.openbus.interceptors {
  internal class ServerInterceptor : InterceptorImpl,
                                     ServerRequestInterceptor {
    #region Fields

    private static readonly ILog Logger =
      LogManager.GetLogger(typeof (ServerInterceptor));

    private static ServerInterceptor _instance;

    internal int ReceivingConnectionSlotId;
    internal int ChainSlotId;

    #endregion

    #region Constructors

    /// <summary>
    /// Inicializa uma nova instância de OpenbusAPI.Interceptors.StandardServerInterceptor.   
    /// </summary>
    private ServerInterceptor()
      : base("ServerInterceptor") {
    }

    #endregion

    internal static ServerInterceptor Instance {
      get { return _instance ?? (_instance = new ServerInterceptor()); }
    }

    #region ServerRequestInterceptor Implemented

    /// <inheritdoc />
    public void receive_request_service_contexts(ServerRequestInfo ri) {
      string interceptedOperation = ri.operation;
      Logger.Info(String.Format(
        "A operação '{0}' foi interceptada no servidor.", interceptedOperation));

      bool legacyContext;
      ServiceContext serviceContext = GetContextFromRequestInfo(ri, true,
                                                                out
                                                                  legacyContext);
      AnyCredential anyCredential = UnmarshalCredential(serviceContext,
                                                        legacyContext);
      Logger.Debug(
        String.Format("A operação '{0}' possui credencial. É legada? {1}.",
                      interceptedOperation, anyCredential.IsLegacy));

      ConnectionImpl conn = null;
      try {
        conn = GetDispatcherForRequest(ri, anyCredential) as ConnectionImpl;
        if (conn == null) {
          Logger.Error(
            "Sem conexão ao barramento, impossível receber a chamada remota.");
          throw new NO_PERMISSION(UnknownBusCode.ConstVal,
                                  CompletionStatus.Completed_No);
        }
        if ((!conn.Legacy) && (anyCredential.IsLegacy)) {
          Logger.Error(
            String.Format(
              "Chamada negada devido a suporte legado inativo: login {0} operação {1} requestId {2}",
              anyCredential.LegacyCredential.identifier, interceptedOperation,
              ri.request_id));
          throw new NO_PERMISSION(0, CompletionStatus.Completed_No);
        }
        Context.SetCurrentConnection(conn, ri);
        conn.ReceiveRequest(ri, anyCredential);
      }
      catch (InvalidSlot e) {
        Logger.Fatal("Falha ao inserir a credencial em seu slot.", e);
        throw;
      }
      finally {
        if (conn != null) {
          ri.set_slot(ReceivingConnectionSlotId, conn);
        }
      }
    }

    /// <inheritdoc />
    public void receive_request(ServerRequestInfo ri) {
      // FIXME: O código abaixo é necessário porque:
      // (1) para que informações possam ser colocadas nos slots do PICurrent, 
      // esse tipo de código deve ficar na receive_request_service_contexts. 
      // No entanto, essas informações acabam não ficando disponíveis para a 
      // send_exception.
      // (2) a send_exception NÃO É CHAMADA caso a exceção seja lançada na 
      // receive_request_service_contexts. A exceção precisa ser lançada aqui.
      // (3) não é possível colocar o código da send_exception nas 
      // receive_request*, pois é impossível chamar ri.add_reply_service_context
      // nesses pontos devido a erro do IIOP.Net.
      try {
        string reset = ri.get_slot(ChainSlotId) as string;
        if ((reset != null) && (reset.Equals("reset"))) {
          throw new NO_PERMISSION(InvalidCredentialCode.ConstVal,
                                  CompletionStatus.Completed_No);
        }
      }
      catch (InvalidSlot e) {
        Logger.Fatal(
          "Falha ao acessar o slot da credencial para avaliar se um reset deve ser enviado.",
          e);
        throw;
      }
      Logger.Info(String.Format(
        "A operação '{0}' será executada.", ri.operation));
    }

    /// <inheritdoc />
    public void send_exception(ServerRequestInfo ri) {
      // esse tratamento precisa ser feito aqui (não é possível na receive_request) por causa de bugs do IIOP.net, descritos em OPENBUS-1677.
      String interceptedOperation = ri.operation;
      Logger.Info(String.Format(
        "O lançamento de uma exceção para a operação '{0}' foi interceptado no servidor.",
        interceptedOperation));

      NO_PERMISSION ex = ri.sending_exception as NO_PERMISSION;
      if (ex == null) {
        return;
      }
      if (ex.Minor == InvalidCredentialCode.ConstVal) {
        try {
          // pela implementação do IIOP.Net, o ServerRequestInfo da send_exception é
          // diferente do existente na receive_request. Assim, não podemos passar a 
          // credencial por um slot e então precisamos fazer o unmarshal novamente.
          ConnectionImpl conn =
            ri.get_slot(ReceivingConnectionSlotId) as ConnectionImpl;
          if (conn == null) {
            Logger.Error(
              "Sem conexão ao barramento, impossível enviar exceção à chamada remota.");
            throw new NO_PERMISSION(UnverifiedLoginCode.ConstVal,
                                    CompletionStatus.Completed_No);
          }
          bool legacyContext;
          ServiceContext serviceContext =
            GetContextFromRequestInfo(ri, conn.Legacy, out legacyContext);
          // credencial é inválida
          AnyCredential anyCredential = UnmarshalCredential(serviceContext,
                                                            legacyContext);
          Logger.Debug(String.Format(
            "A operação '{0}' para a qual será lançada a exceção possui credencial. Legada? {1}.",
            interceptedOperation, anyCredential.IsLegacy));

          conn.SendException(ri, anyCredential);
        }
        catch (InvalidSlot e) {
          Logger.Fatal(
            "Falha ao acessar o slot da conexão de recebimento para enviar uma exceção.",
            e);
          throw;
        }
      }
    }

    #endregion

    #region ServerRequestInterceptor Not Implemented

    /// <inheritdoc />
    public virtual void send_reply(ServerRequestInfo ri) {
      //Nada a ser feito
    }

    /// <inheritdoc />
    public virtual void send_other(ServerRequestInfo ri) {
      //Nada a ser feito
    }

    #endregion

    private Connection GetDispatcherForRequest(ServerRequestInfo request,
                                               AnyCredential credential) {
      Connection dispatcher = null;
      string busId;
      string loginId;
      if (credential.IsLegacy) {
        busId = null;
        loginId = credential.LegacyCredential.identifier;
      }
      else {
        busId = credential.Credential.bus;
        loginId = credential.Credential.login;
      }
      if (Context.OnCallDispatch != null) {
        dispatcher = Context.OnCallDispatch(Context, busId, loginId,
                                            GetObjectUriForObjectKey(
                                              request.object_id),
                                            request.operation);
      }
      if (dispatcher == null) {
        dispatcher = Context.GetDefaultConnection();
      }
      if ((busId != null) && (dispatcher != null) && (!busId.Equals(dispatcher.BusId))) {
        // se a conexão retornada não pertence ao barramento esperado
        Logger.Error(
          String.Format(
            "A conexão retornada pela callback OnCallDispatch não pertence ao barramento da requisição sendo atendida."));
        throw new NO_PERMISSION(UnknownBusCode.ConstVal, CompletionStatus.Completed_No);
      }
      return dispatcher;
    }

    private ServiceContext GetContextFromRequestInfo(RequestInfo ri, bool legacy,
                                                     out bool legacyContext) {
      legacyContext = false;
      String interceptedOperation = ri.operation;
      ServiceContext serviceContext;
      try {
        serviceContext = ri.get_request_service_context(ContextId);
      }
      catch (BAD_PARAM) {
        if (legacy) {
          return GetLegacyContextFromRequestInfo(ri, out legacyContext);
        }
        Logger.Error(String.Format(
          "A chamada à operação '{0}' não possui credencial.",
          interceptedOperation));
        throw new NO_PERMISSION(NoCredentialCode.ConstVal,
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
        Logger.Error(String.Format(
          "A chamada à operação '{0}' não possui credencial.",
          interceptedOperation));
        throw new NO_PERMISSION(NoCredentialCode.ConstVal,
                                CompletionStatus.Completed_No);
      }
      legacyContext = true;
      return serviceContext;
    }

    private AnyCredential UnmarshalCredential(ServiceContext serviceContext,
                                              bool legacyContext) {
      OrbServices orb = OrbServices.GetSingleton();
      if (legacyContext) {
        return UnmarshalLegacyCredential(serviceContext);
      }
      TypeCode credentialTypeCode = orb.create_tc_for_type(typeof(CredentialData));

      byte[] data = serviceContext.context_data;
      return new AnyCredential(
        (CredentialData) InterceptorsInitializer.Codec.decode_value(data, credentialTypeCode));
    }

    private AnyCredential UnmarshalLegacyCredential(
      ServiceContext serviceContext) {
      OrbServices orb = OrbServices.GetSingleton();
      TypeCode credentialTypeCode = orb.create_tc_for_type(typeof(Credential));

      byte[] data = serviceContext.context_data;
      Credential cred =
        (Credential) InterceptorsInitializer.Codec.decode_value(data, credentialTypeCode);
      return new AnyCredential(cred);
    }

    #region Métodos copiados do IIOP.NET

    private static string GetObjectUriForObjectKey(byte[] objectKey) {
      string result = System.Text.Encoding.ASCII.GetString(objectKey);
      return UnescapeNonAscii(result);
    }

    private static string UnescapeNonAscii(string uri) {
      StringBuilder result = null;
      int escapeSequenceStartIndex = 0;
      for (int i = 0; i != uri.Length; ++i) {
        bool endOfSequence;
        if (IsPotentiallyEscapedCharacterRepresentation1(uri, i,
                                                         i -
                                                         escapeSequenceStartIndex,
                                                         out endOfSequence)) {
          // either new escape sequence starting with \ or continue of a sequence
          if (endOfSequence) {
            // it's an escape char in form \uQRST
            if (result == null) {
              result = new StringBuilder(uri, 0, escapeSequenceStartIndex,
                                         uri.Length);
            }
            int charNr = StringConversions.Parse(uri,
                                                 escapeSequenceStartIndex + 2, 4);
            result.Append(Convert.ToChar(charNr));
            escapeSequenceStartIndex = i;
          }
          else {
            continue;
          }
        }
        else if (IsPotentiallyEscapedCharacterRepresentation2(uri, i,
                                                              i -
                                                              escapeSequenceStartIndex,
                                                              out endOfSequence)) {
          if (endOfSequence) {
            // it's an escape char in form \\u
            if (result == null) {
              result = new StringBuilder(uri, 0, escapeSequenceStartIndex,
                                         uri.Length);
            }
            result.Append(@"\u");
            escapeSequenceStartIndex = i;
          }
          else {
            continue;
          }
        }
        else {
          // no escape sequence, add string directly to result
          if (result != null) {
            result.Append(uri, escapeSequenceStartIndex,
                          i - escapeSequenceStartIndex + 1);
          }
          escapeSequenceStartIndex = i;
        }
        ++escapeSequenceStartIndex;
      }
      return result != null ? result.ToString() : uri;
    }

    /// <summary>
    /// checks, if a given candidate sequence may represent an escaped character
    /// </summary>
    /// <returns>true, if possible, otherwise false</returns>
    private static bool IsPotentiallyEscapedCharacterRepresentation1(
      string candidate, int index, int escapedCharRepIndex,
      out bool lastChar) {
      // look for '\uQRST'
      lastChar = escapedCharRepIndex == 5;
      switch (escapedCharRepIndex) {
        case 0:
          return candidate[index] == '\\';
        case 1:
          return candidate[index] == 'u';
        case 2:
        case 3:
        case 4:
        case 5:
          return Char.IsDigit(candidate, index) ||
                 (candidate[index] >= 'A' && candidate[index] <= 'F') ||
                 (candidate[index] >= 'a' && candidate[index] <= 'f');
        default:
          return false;
      }
    }

    private static bool IsPotentiallyEscapedCharacterRepresentation2(
      string candidate, int index, int escapedCharRepIndex,
      out bool lastChar) {
      lastChar = false;
      // look for \\u
      switch (escapedCharRepIndex) {
        case 0:
        case 1:
          return candidate[index] == '\\';
        case 2:
          lastChar = true;
          return candidate[index] == 'u';
        default:
          return false;
      }
    }

    #endregion
                                     }
}