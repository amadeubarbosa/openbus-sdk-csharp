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
    internal int NoInvalidLoginHandlingSlotId;

    #endregion

    #region Contructor

    /// <summary>
    ///   Inicializa uma nova inst�ncia de OpenbusAPI.Interceptors.StandardClientInterceptor
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
    ///   Intercepta o request para inser��o de informa��o de contexto.
    /// </summary>
    /// <remarks>Informa��o do cliente</remarks>
    public void send_request(ClientRequestInfo ri) {
      if (!Context.IsCurrentThreadIgnored(ri)) {
        ConnectionImpl conn = GetCurrentConnection() as ConnectionImpl;
        if (conn != null) {
          conn.SendRequest(ri);
          return;
        }
        Logger.Error(
          "Sem conex�o ao barramento, imposs�vel realizar a chamada remota.");
        throw new NO_PERMISSION(NoLoginCode.ConstVal,
          CompletionStatus.Completed_No);
      }
      Logger.Debug("O login est� sendo ignorado para esta chamada.");
    }

    /// <inheritdoc />
    public void receive_exception(ClientRequestInfo ri) {
      if (!Context.IsCurrentThreadIgnored(ri)) {
        ConnectionImpl conn = GetCurrentConnection() as ConnectionImpl;
        if (conn != null) {
          conn.ReceiveException(ri);
          return;
        }
        Logger.Warn("Sem conex�o ao barramento para receber uma exce��o.");
      }
      Logger.Debug("O login est� sendo ignorado para receber uma exce��o.");
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
          "Imposs�vel retornar conex�o corrente, pois n�o foi definida.");
        throw new NO_PERMISSION(NoLoginCode.ConstVal,
          CompletionStatus.Completed_No);
      }
      return conn;
    }
  }
}