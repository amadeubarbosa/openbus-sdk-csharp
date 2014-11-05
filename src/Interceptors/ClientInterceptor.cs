using log4net;
using omg.org.CORBA;
using omg.org.PortableInterceptor;
using tecgraf.openbus.core.v2_1.services.access_control;

namespace tecgraf.openbus.interceptors {
  /// <summary>
  ///   Representa o interceptador cliente.
  ///   Implementa PortableInterceptor.ClientRequestInterceptor.
  /// </summary>
  internal class ClientInterceptor : InterceptorImpl,
    ClientRequestInterceptor {
    #region Fields

    private static readonly ILog Logger =
      LogManager.GetLogger(typeof (ClientInterceptor));

    private static ClientInterceptor _instance;

    internal int LoginSlotId;
    internal int InvalidLoginSlotId;

    #endregion

    #region Contructor

    /// <summary>
    ///   Inicializa uma nova instância de OpenbusAPI.Interceptors.StandardClientInterceptor
    /// </summary>
    private ClientInterceptor()
      : base("ClientInterceptor") {
    }

    internal static ClientInterceptor Instance {
      get { return _instance ?? (_instance = new ClientInterceptor()); }
    }

    #endregion

    #region ClientRequestInterceptor Members

    /// <summary>
    ///   Intercepta o request para inserção de informação de contexto.
    /// </summary>
    /// <remarks>Informação do cliente</remarks>
    public void send_request(ClientRequestInfo ri) {
      if (!Context.IsCurrentThreadIgnored(ri)) {
        ConnectionImpl conn = GetCurrentConnection() as ConnectionImpl;
        if (conn != null) {
          conn.SendRequest(ri);
          return;
        }
        Logger.Error(
          "Sem conexão ao barramento, impossível realizar a chamada remota.");
        throw new NO_PERMISSION(NoLoginCode.ConstVal,
          CompletionStatus.Completed_No);
      }
      Logger.Debug("O login está sendo ignorado para esta chamada.");
    }

    /// <inheritdoc />
    public void receive_exception(ClientRequestInfo ri) {
      if (!Context.IsCurrentThreadIgnored(ri)) {
        ConnectionImpl conn = GetCurrentConnection() as ConnectionImpl;
        if (conn != null) {
          conn.ReceiveException(ri);
          return;
        }
        Logger.Warn("Sem conexão ao barramento para receber uma exceção.");
      }
      Logger.Debug("O login está sendo ignorado para receber uma exceção.");
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

    private Connection GetCurrentConnection() {
      Connection conn = Context.GetCurrentConnection();
      if (conn == null) {
        Logger.Error(
          "Impossível retornar conexão corrente, pois não foi definida.");
        throw new NO_PERMISSION(NoLoginCode.ConstVal,
          CompletionStatus.Completed_No);
      }
      return conn;
    }
  }
}