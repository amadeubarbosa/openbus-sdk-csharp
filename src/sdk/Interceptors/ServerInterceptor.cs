using log4net;
using omg.org.CORBA;
using omg.org.PortableInterceptor;
using tecgraf.openbus.core.v2_00.services.access_control;

namespace tecgraf.openbus.sdk.interceptors {
  internal class ServerInterceptor : InterceptorImpl,
                                             ServerRequestInterceptor {
    #region Fields

    private static readonly ILog Logger =
      LogManager.GetLogger(typeof (ServerInterceptor));

    private static ServerInterceptor _instance;
    private ConnectionImpl _connection;
    private readonly LoginCache _loginsCache;

    internal bool IsMultiplexed;

    #endregion

    #region Constructors

    /// <summary>
    /// Inicializa uma nova instância de OpenbusAPI.Interceptors.StandardServerInterceptor.   
    /// </summary>
    private ServerInterceptor()
      : base("ServerInterceptor") {
      _loginsCache = new LoginCache();
    }

    #endregion

    internal ConnectionImpl Connection {
      get { return _connection;  }
      set {
        _connection = value;
        if (value == null) {
          //TODO: inutilizar conexão
        }
        else {
          _connection.SetLoginsCache(_loginsCache);
        }
      }
    }

    internal static ServerInterceptor Instance {
      get { return _instance ?? (_instance = new ServerInterceptor()); }
    }

    #region ServerRequestInterceptor Implemented

    /// <inheritdoc />
    public void receive_request(ServerRequestInfo ri) {
      if (Connection != null) {
        Connection.ReceiveRequest(ri);
        return;
      }
      Logger.Fatal(
        "Sem conexão ao barramento, impossível receber a chamada remota.");
      throw new NO_PERMISSION(UnverifiedLoginCode.ConstVal,
                              CompletionStatus.Completed_No);
    }

    /// <inheritdoc />
    public void send_exception(ServerRequestInfo ri) {
      if (Connection != null) {
        Connection.SendException(ri);
        return;
      }
      Logger.Fatal(
        "Sem conexão ao barramento, impossível enviar exceção à chamada remota.");
      throw new NO_PERMISSION(UnverifiedLoginCode.ConstVal,
                              CompletionStatus.Completed_No);
    }

    /// <inheritdoc />
    public virtual void send_reply(ServerRequestInfo ri) {
      if (Connection != null) {
        Connection.SendReply(ri);
        return;
      }
      Logger.Fatal(
        "Sem conexão ao barramento, impossível enviar resposta à chamada remota.");
      throw new NO_PERMISSION(UnverifiedLoginCode.ConstVal,
                              CompletionStatus.Completed_No);
    }

    #endregion

    #region ServerRequestInterceptor Not Implemented

    /// <inheritdoc />
    public virtual void receive_request_service_contexts(ServerRequestInfo ri) {
      //Nada a ser feito
    }

    /// <inheritdoc />
    public virtual void send_other(ServerRequestInfo ri) {
      //Nada a ser feito
    }

    #endregion
  }
}