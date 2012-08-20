using log4net;
using omg.org.IOP;

namespace tecgraf.openbus.interceptors {
  /// <summary>
  /// Classe responsável por inicializar os interceptadores.
  /// Implementa PortableInterceptor.ORBInitializer.
  /// </summary>
  internal class InterceptorsInitializer :
    omg.org.PortableInterceptor.ORBInitializer {
    #region Field

    private static readonly ILog Logger =
      LogManager.GetLogger(typeof (InterceptorsInitializer));

    public CallContextImpl Context;

    #endregion

    #region ORBInitializer Members

    /// <inheritdoc />
    public void pre_init(omg.org.PortableInterceptor.ORBInitInfo info) {
      int credentialSlotId = info.allocate_slot_id();
      int currentThreadSlotId = info.allocate_slot_id();
      int connectionSlotId = info.allocate_slot_id();
      int receivingSlotId = info.allocate_slot_id();
      int loginSlotId = info.allocate_slot_id();
      int ignoreThreadSlotId = info.allocate_slot_id();
      int joinedChainSlotId = info.allocate_slot_id();
      Context = new CallContextImpl(currentThreadSlotId, ignoreThreadSlotId,
                                    joinedChainSlotId);

      Codec codec = info.codec_factory.create_codec(
        new Encoding(ENCODING_CDR_ENCAPS.ConstVal, 1, 2));
      ServerInterceptor.Instance.Codec = codec;
      ServerInterceptor.Instance.CredentialSlotId = credentialSlotId;
      ServerInterceptor.Instance.ConnectionSlotId = connectionSlotId;
      ServerInterceptor.Instance.ReceivingConnectionSlotId = receivingSlotId;
      ServerInterceptor.Instance.Context = Context;
      ClientInterceptor.Instance.Codec = codec;
      ClientInterceptor.Instance.CredentialSlotId = credentialSlotId;
      ClientInterceptor.Instance.ConnectionSlotId = connectionSlotId;
      ClientInterceptor.Instance.LoginSlotId = loginSlotId;
      ClientInterceptor.Instance.Context = Context;

      info.add_server_request_interceptor(ServerInterceptor.Instance);
      Logger.Info("Interceptador servidor registrado.");
      info.add_client_request_interceptor(ClientInterceptor.Instance);
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