using log4net;
using omg.org.IOP;

namespace tecgraf.openbus.sdk.standard.interceptors
{
  /// <summary>
  /// Classe responsável por inicializar o interceptador cliente.
  /// Implementa ProtableInterceptor.ORBInitializer.
  /// </summary>
  internal class StandardClientInitializer : omg.org.PortableInterceptor.ORBInitializer
  {
    #region Field

    private static readonly ILog Logger = LogManager.GetLogger(typeof(StandardClientInitializer));

    #endregion

    #region ORBInitializer Members

    /// <inheritdoc />
    public void pre_init(omg.org.PortableInterceptor.ORBInitInfo info) {
      Codec codec = info.codec_factory.create_codec(
                        new Encoding(ENCODING_CDR_ENCAPS.ConstVal, 1, 2));
      StandardClientInterceptor.Instance.Codec = codec;
      info.add_client_request_interceptor(StandardClientInterceptor.Instance);

      Logger.Info("Interceptador cliente registrado.");
    }

    #endregion

    #region ORBInitializer Not Implemented

    /// <inheritdoc />
    public void post_init(omg.org.PortableInterceptor.ORBInitInfo info) {
    }

    #endregion
  }
}
