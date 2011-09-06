using log4net;
using omg.org.IOP;


namespace Tecgraf.Openbus.Interceptors
{
  /// <summary>
  /// Classe responsável por inicializar o interceptador cliente.
  /// Implementa ProtableInterceptor.ORBInitializer.
  /// </summary>
  internal class ClientInitializer : omg.org.PortableInterceptor.ORBInitializer
  {
    #region Field

    private static ILog logger = LogManager.GetLogger(typeof(ClientInitializer));

    /// <summary>
    /// Sinaliza se o interceptador é tolerante a falha.
    /// </summary>
    private bool isFaultTolerant;

    #endregion

    #region Constructor

    /// <summary>
    /// Construtor.
    /// </summary>
    /// <param name="isFaultTolerant">
    /// Sinaliza se o interceptador será tolerante a falhas.
    /// </param>
    public ClientInitializer(bool isFaultTolerant) {
      this.isFaultTolerant = isFaultTolerant;
    }

    #endregion

    #region ORBInitializer Members

    /// <inheritdoc />
    public void post_init(omg.org.PortableInterceptor.ORBInitInfo info) {
      try {
        Encoding encode = new Encoding(ENCODING_CDR_ENCAPS.ConstVal, 1, 2);
        Codec codec = info.codec_factory.create_codec(encode);
        if (isFaultTolerant) {
          info.add_client_request_interceptor(new FTClientInterceptor(codec));
        }
        else {
          info.add_client_request_interceptor(new ClientInterceptor(codec));
        }

        logger.Info("Registrei interceptador cliente.");
      }
      catch (System.Exception e) {
        logger.Error("Erro no registro do interceptador cliente", e);
      }
    }

    #endregion

    #region ORBInitializer Not Implemented

    /// <inheritdoc />
    public void pre_init(omg.org.PortableInterceptor.ORBInitInfo info) {
      //Nada a fazer
    }

    #endregion
  }
}
