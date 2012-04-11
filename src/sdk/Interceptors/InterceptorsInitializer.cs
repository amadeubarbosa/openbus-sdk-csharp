using log4net;
using omg.org.IOP;
using tecgraf.openbus.sdk.multiplexed;

namespace tecgraf.openbus.sdk.interceptors
{
  /// <summary>
  /// Classe responsável por inicializar os interceptadores.
  /// Implementa PortableInterceptor.ORBInitializer.
  /// </summary>
  internal class InterceptorsInitializer : omg.org.PortableInterceptor.ORBInitializer
  {
    #region Field

    private static readonly ILog Logger = LogManager.GetLogger(typeof(InterceptorsInitializer));

    private readonly bool _isMultiplexed;
    public ConnectionMultiplexerImpl Multiplexer;

    private int _credentialSlotId;
    private int _currentThreadSlotId;
    private int _joinedChainSlotId;
    private int _connectionSlotId;

    #endregion

    #region Constructors

    public InterceptorsInitializer(bool isMultiplexed) {
      _isMultiplexed = isMultiplexed;
    }

    #endregion

    #region ORBInitializer Members

    /// <inheritdoc />
    public void pre_init(omg.org.PortableInterceptor.ORBInitInfo info) {
      _credentialSlotId = info.allocate_slot_id();
      _currentThreadSlotId = info.allocate_slot_id();
      _joinedChainSlotId = info.allocate_slot_id();
      _connectionSlotId = info.allocate_slot_id();
      if (_isMultiplexed) {
        Multiplexer = new ConnectionMultiplexerImpl(_currentThreadSlotId);
      }
      Codec codec = info.codec_factory.create_codec(
                        new Encoding(ENCODING_CDR_ENCAPS.ConstVal, 1, 2));
      ServerInterceptor.Instance.Codec = codec;
      ServerInterceptor.Instance.IsMultiplexed = _isMultiplexed;
      ClientInterceptor.Instance.Codec = codec;
      ClientInterceptor.Instance.IsMultiplexed = _isMultiplexed;
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
