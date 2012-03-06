using log4net;
using omg.org.CORBA;
using omg.org.IOP;
using omg.org.PortableInterceptor;
using tecgraf.openbus.core.v2_00.services.access_control;
using tecgraf.openbus.sdk.Interceptors;

namespace tecgraf.openbus.sdk.Standard.Interceptors {
  internal class StandardServerInterceptor : InterceptorImpl,
                                             ServerRequestInterceptor {
    #region Fields

    private static readonly ILog Logger =
      LogManager.GetLogger(typeof (StandardServerInterceptor));

    private static StandardServerInterceptor _instance;

    private static Codec _codec;

    private StandardConnection _connection;

    #endregion

    #region Constructors

    /// <summary>
    /// Inicializa uma nova instância de OpenbusAPI.Interceptors.StandardServerInterceptor.   
    /// </summary>
    /// <param name="codec">Codificador.</param>
    internal StandardServerInterceptor(Codec codec)
      : base("StandardServerInterceptor", codec) {
      _codec = codec;
    }

    #endregion

    internal StandardConnection Connection { get; set; }

    internal static StandardServerInterceptor Instance {
      get { return _instance ?? (_instance = new StandardServerInterceptor(_codec)); }
    }

    #region ServerRequestInterceptor Implemented

    /// <inheritdoc />
    public void receive_request(ServerRequestInfo ri) {
      //TODO: talvez remover os metodos de interceptacao da classe connection para uma outra classe ou pra cá de volta e passar a connection aqui? Só da pra saber direito o melhor formato quando implementar a multiplexação...
      if (_connection != null) {
        _connection.ReceiveRequest(ri);
        return;
      }
      Logger.Fatal("Sem conexão ao barramento, impossível receber a chamada remota.");
      throw new NO_PERMISSION(UnverifiedLoginCode.ConstVal, CompletionStatus.Completed_No);
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

  }
}