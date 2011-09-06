using log4net;
using omg.org.IOP;
using omg.org.PortableInterceptor;
using tecgraf.openbus.core.v1_05.access_control_service;


namespace Tecgraf.Openbus.Interceptors
{
  /// <summary>
  /// Representa o interceptador cliente.
  /// Implementa PortableInterceptor.ClientRequestInterceptor.
  /// </summary>
  internal class ClientInterceptor : InterceptorImpl, ClientRequestInterceptor
  {
    #region Fields

    private static ILog logger = LogManager.GetLogger(typeof(ClientInterceptor));

    #endregion

    #region Contructor

    /// <summary>
    /// Inicializa uma nova inst�ncia de OpenbusAPI.Interceptors.ClientInterceptor
    /// </summary>
    /// <param name="codec">Codificador</param>
    public ClientInterceptor(Codec codec)
      : base("ClientInterceptor", codec) {
    }

    #endregion

    #region ClientRequestInterceptor Members

    /// <summary>
    /// Intercepta o request para inser��o de informa��o de contexto.
    /// </summary>
    /// <remarks>Informa��o do cliente</remarks>
    public void send_request(ClientRequestInfo ri) {
      logger.Debug("executando m�todo: " + ri.operation);

      /* Verifica se existe uma credencial para envio */
      Openbus openbus = Openbus.GetInstance();
      Credential credential = openbus.Credential;
      if (string.IsNullOrEmpty(credential.identifier)) {
        logger.Info("Sem Credencial!");
        return;
      }

      logger.Debug("Tem Credencial");

      byte[] value = null;
      try {
        value = this.Codec.encode_value(credential);
      }
      catch {
        logger.Fatal("Erro na codifica��o da credencial.");
      }
      ServiceContext serviceContext = new ServiceContext(CONTEXT_ID, value);
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
