using System;
using System.Threading;
using Ch.Elca.Iiop.Idl;
using log4net;
using omg.org.CORBA;
using omg.org.IOP;
using omg.org.PortableInterceptor;
using tecgraf.openbus.core.v1_05.access_control_service;
using tecgraf.openbus.core.v2_0.credential;
using tecgraf.openbus.core.v2_0.services.access_control;
using tecgraf.openbus.exceptions;
using TypeCode = omg.org.CORBA.TypeCode;

namespace tecgraf.openbus.interceptors {
  internal class ServerInterceptor : InterceptorImpl,
                                     ServerRequestInterceptor {
    #region Fields

    private static readonly ILog Logger =
      LogManager.GetLogger(typeof (ServerInterceptor));

    private static ServerInterceptor _instance;

    internal int ReceivingConnectionSlotId;

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
      ServiceContext serviceContext = GetContextFromRequestInfo(ri,
                                                                out
                                                                  legacyContext);
      AnyCredential anyCredential = UnmarshalCredential(serviceContext,
                                                        legacyContext);
      Logger.Info(
        String.Format("A operação '{0}' possui credencial. É legada? {1}.",
                      interceptedOperation, anyCredential.IsLegacy));

      // insere a credencial no slot para a getCallerChain e send_exception usarem
      ConnectionImpl conn = null;
      try {
        ri.set_slot(CredentialSlotId, anyCredential);
        string busId = string.Empty;
        if (!anyCredential.IsLegacy) {
          busId = anyCredential.Credential.bus;
        }
        else {
          bool valid = false;
          foreach (Connection incoming in Manager.GetIncomingConnections()) {
            conn = incoming as ConnectionImpl;
            if ((conn == null) || (!conn.Legacy)) {
              continue;
            }
            SetCurrentConnection(ri, conn);
            Manager.Requester = conn;
            try {
              if (Manager.LoginsCache.ValidateLogin(anyCredential, conn)) {
                valid = true;
                busId = conn.BusId;
                break;
              }
            }
            catch (Exception e) {
              const string message = "Erro ao validar o login 1.5.";
              Logger.Error(message, e);
            }
          }
          if (!valid) {
            Logger.Fatal(
              "Não foi possível encontrar um barramento que aceite a credencial recebida.");
            throw new NO_PERMISSION(0, CompletionStatus.Completed_No);
          }
        }
        conn = Manager.GetDispatcher(busId) as ConnectionImpl;
        if (conn == null) {
          conn = Manager.DefaultConnection as ConnectionImpl;
          if (conn == null) {
            Logger.Fatal(
              "Sem conexão ao barramento, impossível receber a chamada remota.");
            throw new NO_PERMISSION(UnknownBusCode.ConstVal,
                                    CompletionStatus.Completed_No);
          }
        }
        SetCurrentConnection(ri, conn);
        Manager.Requester = conn;
        conn.ReceiveRequest(ri, anyCredential);
      }
      catch (InvalidSlot e) {
        const string msg = "Falha ao inserir a credencial em seu slot.";
        Logger.Fatal(msg, e);
        throw new OpenBusException(msg, e);
      }
      finally {
        RemoveCurrentConnection(ri);
        // Talvez a linha abaixo não seja necessaria, pois o PICurrent deveria
        // acabar junto com a thread de interceptação (Manager.Requester usa PICurrent).
        Manager.Requester = null;
        if (conn != null) {
          ri.set_slot(ReceivingConnectionSlotId, conn);
        }
      }
    }

    /// <inheritdoc />
    public void receive_request(ServerRequestInfo ri) {
      // TODO FIXME: O código abaixo é necessário porque:
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
        string reset = ri.get_slot(CredentialSlotId) as string;
        if ((reset != null) && (reset.Equals("reset"))) {
          throw new NO_PERMISSION(InvalidCredentialCode.ConstVal,
                            CompletionStatus.Completed_No);
        }
      }
      catch (InvalidSlot e) {
        const string msg = "Falha ao acessar o slot da credencial para avaliar se um reset deve ser enviado.";
        Logger.Fatal(msg, e);
        throw new OpenBusException(msg, e);
      }
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
          bool legacyContext;
          ServiceContext serviceContext =
            GetContextFromRequestInfo(ri, out legacyContext);
          // credencial é inválida
          AnyCredential anyCredential = UnmarshalCredential(serviceContext,
                                                            legacyContext);
          Logger.Info(String.Format(
            "A operação '{0}' para a qual será lançada a exceção possui credencial. Legada? {1}.",
            interceptedOperation, anyCredential.IsLegacy));

          try {
            ConnectionImpl conn = ri.get_slot(ReceivingConnectionSlotId) as ConnectionImpl;
            if (conn != null) {
              conn.SendException(ri, anyCredential);
              return;
            }
          }
          catch (InvalidSlot e) {
            const string msg = "Falha ao acessar o slot da conexão de recebimento para enviar uma exceção.";
            Logger.Fatal(msg, e);
            throw new OpenBusException(msg, e);
          }
          Logger.Fatal(
            "Sem conexão ao barramento, impossível enviar exceção à chamada remota.");
          throw new NO_PERMISSION(UnverifiedLoginCode.ConstVal,
                                  CompletionStatus.Completed_No);
        }
        finally {
          ClearRequest(ri);
        }
      }
    }

    /// <inheritdoc />
    public virtual void send_reply(ServerRequestInfo ri) {
      ClearRequest(ri);
    }

    #endregion

    #region ServerRequestInterceptor Not Implemented

    /// <inheritdoc />
    public virtual void send_other(ServerRequestInfo ri) {
      //Nada a ser feito
    }

    #endregion

    private void ClearRequest(ServerRequestInfo ri) {
      try {
        ri.set_slot(ReceivingConnectionSlotId, null);
        ri.set_slot(CredentialSlotId, null);
        ri.set_slot(ConnectionSlotId, null);
      }
      catch (InvalidSlot e) {
        const string message =
          "Falha inesperada ao limpar informações nos slots";
        Logger.Fatal(message, e);
        throw new OpenBusException(message, e);
      }
    }

    private void SetCurrentConnection(ServerRequestInfo ri, ConnectionImpl conn) {
      int id = Thread.CurrentThread.ManagedThreadId;
      try {
        ri.set_slot(Manager.CurrentThreadSlotId, id);
      }
      catch (InvalidSlot e) {
        const string message =
          "Falha inesperada ao acessar o slot da thread corrente";
        Logger.Fatal(message, e);
        throw new OpenBusException(message, e);
      }
      Manager.SetConnectionByThreadId(id, conn);
    }

    private void RemoveCurrentConnection(ServerRequestInfo ri) {
      try {
        ri.set_slot(Manager.CurrentThreadSlotId, null);
      }
      catch (InvalidSlot e) {
        const string message =
          "Falha inesperada ao acessar o slot da thread corrente";
        Logger.Fatal(message, e);
        throw new OpenBusException(message, e);
      }
      int id = Thread.CurrentThread.ManagedThreadId;
      Manager.SetConnectionByThreadId(id, null);
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
        if (Legacy) {
          return GetLegacyContextFromRequestInfo(ri, out legacyContext);
        }
        Logger.Warn(String.Format(
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
        Logger.Warn(String.Format(
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
      Type credentialType = typeof (CredentialData);
      TypeCode credentialTypeCode =
        orb.create_interface_tc(Repository.GetRepositoryID(credentialType),
                                credentialType.Name);

      byte[] data = serviceContext.context_data;
      return new AnyCredential(
        (CredentialData) Codec.decode_value(data, credentialTypeCode));
    }

    private AnyCredential UnmarshalLegacyCredential(
      ServiceContext serviceContext) {
      OrbServices orb = OrbServices.GetSingleton();
      Type credentialType = typeof (Credential);
      TypeCode credentialTypeCode =
        orb.create_interface_tc(Repository.GetRepositoryID(credentialType),
                                credentialType.Name);

      byte[] data = serviceContext.context_data;
      Credential cred =
        (Credential) Codec.decode_value(data, credentialTypeCode);
      return new AnyCredential(cred);
    }
                                     }
}