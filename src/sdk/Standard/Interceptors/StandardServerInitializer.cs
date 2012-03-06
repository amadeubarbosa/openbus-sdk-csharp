using log4net;
using omg.org.IOP;

namespace tecgraf.openbus.sdk.Standard.Interceptors
{
  internal class StandardServerInitializer : omg.org.PortableInterceptor.ORBInitializer
  {
    #region Fields

    private static readonly ILog Logger = LogManager.GetLogger(typeof(StandardServerInitializer));

    #endregion

    #region ORBInitializer Members

    /// <inheritdoc />
    public void post_init(omg.org.PortableInterceptor.ORBInitInfo info) {
      CodecFactory codecFactory = info.codec_factory;
      Encoding encode = new Encoding();
      Codec codec = codecFactory.create_codec(encode);
      info.add_server_request_interceptor(new StandardServerInterceptor(codec));

      Logger.Info("O interceptador servidor foi registrado.");
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
