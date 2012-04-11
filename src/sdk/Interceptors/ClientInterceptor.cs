using log4net;
using omg.org.CORBA;
using omg.org.PortableInterceptor;
using tecgraf.openbus.core.v2_00.services.access_control;

namespace tecgraf.openbus.sdk.interceptors {
  /// <summary>
  /// Representa o interceptador cliente.
  /// Implementa PortableInterceptor.ClientRequestInterceptor.
  /// </summary>
  internal class ClientInterceptor : InterceptorImpl,
                                             ClientRequestInterceptor {
    #region Fields

    private static readonly ILog Logger =
      LogManager.GetLogger(typeof(ClientInterceptor));

    private static ClientInterceptor _instance;
    internal bool IsMultiplexed;

    #endregion

    #region Contructor

    /// <summary>
    /// Inicializa uma nova instância de OpenbusAPI.Interceptors.StandardClientInterceptor
    /// </summary>
    private ClientInterceptor()
      : base("ClientInterceptor") {
    }

    internal ConnectionImpl Connection { get; set; }

    internal static ClientInterceptor Instance {
      get { return _instance ?? (_instance = new ClientInterceptor()); }
    }

    #endregion

    #region ClientRequestInterceptor Members

    /// <summary>
    /// Intercepta o request para inserção de informação de contexto.
    /// </summary>
    /// <remarks>Informação do cliente</remarks>
    public void send_request(ClientRequestInfo ri) {
      if (Connection != null) {
        Connection.SendRequest(ri);
        return;
      }
      Logger.Fatal("Sem conexão ao barramento, impossível realizar a chamada remota.");
      throw new NO_PERMISSION(NoLoginCode.ConstVal, CompletionStatus.Completed_No);
    }

    /// <inheritdoc />
    public void receive_exception(ClientRequestInfo ri) {
      if (Connection != null) {
        Connection.ReceiveException(ri);
      }
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
  }
}