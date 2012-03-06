using log4net;
using omg.org.CORBA;
using omg.org.IOP;
using omg.org.PortableInterceptor;
using tecgraf.openbus.core.v2_00.services.access_control;
using tecgraf.openbus.sdk.Interceptors;

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

    private static StandardClientInterceptor _instance;

    private static Codec _codec;

    private StandardConnection _connection;

    #endregion

    #region Contructor

    /// <summary>
    /// Inicializa uma nova instância de OpenbusAPI.Interceptors.StandardClientInterceptor
    /// </summary>
    /// <param name="codec">Codificador.</param>
    internal StandardClientInterceptor(Codec codec)
      : base("StandardClientInterceptor", codec) {
      _codec = codec;
    }

    internal StandardConnection Connection { get; set; }

    internal static StandardClientInterceptor Instance {
      get { return _instance ?? (_instance = new StandardClientInterceptor(_codec)); }
    }

    #endregion

    #region ClientRequestInterceptor Members

    /// <summary>
    /// Intercepta o request para inserção de informação de contexto.
    /// </summary>
    /// <remarks>Informação do cliente</remarks>
    public void send_request(ClientRequestInfo ri) {
      //TODO: talvez remover os metodos de interceptacao da classe connection para uma outra classe ou pra cá de volta e passar a connection aqui? Só da pra saber direito o melhor formato quando implementar a multiplexação...
      if (_connection != null) {
        _connection.SendRequest(ri);
        return;
      }
      Logger.Fatal("Sem conexão ao barramento, impossível realizar a chamada remota.");
      throw new NO_PERMISSION(NoLoginCode.ConstVal, CompletionStatus.Completed_No);
    }

    /// <inheritdoc />
    public void receive_exception(ClientRequestInfo ri) {
      if (_connection != null) {
        _connection.ReceiveException(ri);
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