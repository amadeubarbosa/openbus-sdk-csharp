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

    public ConnectionManagerImpl Manager;

    #endregion

    #region ORBInitializer Members

    /// <inheritdoc />
    public void pre_init(omg.org.PortableInterceptor.ORBInitInfo info) {
      const bool legacy = true;
      int credentialSlotId = info.allocate_slot_id();
      int currentThreadSlotId = info.allocate_slot_id();
      int connectionSlotId = info.allocate_slot_id();
      int receivingSlotId = info.allocate_slot_id();
      int loginSlotId = info.allocate_slot_id();
      int ignoreThreadSlotId = info.allocate_slot_id();
      int joinedChainSlotId = info.allocate_slot_id();
      Manager = new ConnectionManagerImpl(currentThreadSlotId,
                                          ignoreThreadSlotId, legacy);

      Codec codec = info.codec_factory.create_codec(
        new Encoding(ENCODING_CDR_ENCAPS.ConstVal, 1, 2));
      ServerInterceptor.Instance.Codec = codec;
      ServerInterceptor.Instance.CredentialSlotId = credentialSlotId;
      ServerInterceptor.Instance.ConnectionSlotId = connectionSlotId;
      ServerInterceptor.Instance.ReceivingConnectionSlotId = receivingSlotId;
      ServerInterceptor.Instance.Manager = Manager;
      ServerInterceptor.Instance.Legacy = legacy;
      ClientInterceptor.Instance.Codec = codec;
      ClientInterceptor.Instance.CredentialSlotId = credentialSlotId;
      ClientInterceptor.Instance.ConnectionSlotId = connectionSlotId;
      ClientInterceptor.Instance.JoinedChainSlotId = joinedChainSlotId;
      ClientInterceptor.Instance.LoginSlotId = loginSlotId;
      ClientInterceptor.Instance.Manager = Manager;
      ClientInterceptor.Instance.Legacy = legacy;

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