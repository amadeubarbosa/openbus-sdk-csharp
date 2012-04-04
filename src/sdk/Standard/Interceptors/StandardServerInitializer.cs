using log4net;
using omg.org.IOP;

namespace tecgraf.openbus.sdk.standard.interceptors
{
  internal class StandardServerInitializer : omg.org.PortableInterceptor.ORBInitializer
  {
    #region Fields

    private static readonly ILog Logger = LogManager.GetLogger(typeof(StandardServerInitializer));

    #endregion

    #region ORBInitializer Members

    /// <inheritdoc />
    public void pre_init(omg.org.PortableInterceptor.ORBInitInfo info) {
      Codec codec = info.codec_factory.create_codec(
                        new Encoding(ENCODING_CDR_ENCAPS.ConstVal, 1, 2));
      StandardServerInterceptor.Instance.Codec = codec;
      info.add_server_request_interceptor(StandardServerInterceptor.Instance);

      Logger.Info("O interceptador servidor foi registrado.");
    }

    #endregion

    #region ORBInitializer Not Implemented

    /// <inheritdoc />
    public void post_init(omg.org.PortableInterceptor.ORBInitInfo info) {
    }

    #endregion
  }
}
