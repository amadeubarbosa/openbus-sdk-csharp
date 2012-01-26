using log4net;
using omg.org.IOP;
using omg.org.PortableInterceptor;
using tecgraf.openbus.core.v2_00.credential;
using tecgraf.openbus.sdk.Interceptors;

namespace tecgraf.openbus.sdk.Standard.Interceptors
{
  /// <summary>
  /// Representa o interceptador cliente.
  /// Implementa PortableInterceptor.ClientRequestInterceptor.
  /// </summary>
  internal class StandardClientInterceptor : InterceptorImpl, ClientRequestInterceptor
  {
    #region Fields

    private static readonly ILog Logger = LogManager.GetLogger(typeof(StandardClientInterceptor));
    private StandardOpenbus _bus;

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
    }

    #endregion

    #region ClientRequestInterceptor Members

    /// <summary>
    /// Intercepta o request para inser��o de informa��o de contexto.
    /// </summary>
    /// <remarks>Informa��o do cliente</remarks>
    public void send_request(ClientRequestInfo ri) {
      Logger.Debug("executando m�todo: " + ri.operation);

      /* Verifica se existe uma credencial para envio */
      CredentialData data = _bus.Connect().Credential;
      if (string.IsNullOrEmpty(credential.identifier)) {
        Logger.Info("Sem Credencial!");
        return;
      }

      Logger.Debug("Tem Credencial");

      byte[] value = null;
      try {
        value = this.Codec.encode_value(credential);
      }
      catch {
        Logger.Fatal("Erro na codifica��o da credencial.");
      }
      ServiceContext serviceContext = new ServiceContext(ContextId, value);
      ri.add_request_service_context(serviceContext, false);
    }

    #endregion

    #region ClientRequestInterceptor Not Implemented

    /// <inheritdoc />
    public virtual void receive_exception(ClientRequestInfo ri) {
      //Nada a ser feito;
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

  }
}
