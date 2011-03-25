using omg.org.IOP;
using OpenbusAPI.Logger;

namespace OpenbusAPI.Interceptors
{
  internal class ServerInitializer : omg.org.PortableInterceptor.ORBInitializer
  {

    #region ORBInitializer Members

    /// <inheritdoc />
    public void post_init(omg.org.PortableInterceptor.ORBInitInfo info) {
      CodecFactory codecFactory = info.codec_factory;
      Encoding encode = new omg.org.IOP.Encoding();
      Codec codec = codecFactory.create_codec(encode);
      int slotId = info.allocate_slot_id();
      info.add_server_request_interceptor(new ServerInterceptor(codec, slotId));

      Log.INTERCEPTORS.Info("O interceptador servidor foi registrado.");
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
