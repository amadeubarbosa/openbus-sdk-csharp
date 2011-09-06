using log4net;
using omg.org.IOP;


namespace Tecgraf.Openbus.Interceptors
{
  internal class ServerInitializer : omg.org.PortableInterceptor.ORBInitializer
  {
    #region Fields

    private static ILog logger = LogManager.GetLogger(typeof(ServerInitializer));

    private CredentialValidationPolicy policy;

    #endregion

    #region Constructor

    /// <summary>
    /// Construtor.
    /// </summary>
    public ServerInitializer(CredentialValidationPolicy policy) {
      this.policy = policy;
    }

    #endregion

    #region ORBInitializer Members

    /// <inheritdoc />
    public void post_init(omg.org.PortableInterceptor.ORBInitInfo info) {
      CodecFactory codecFactory = info.codec_factory;
      Encoding encode = new omg.org.IOP.Encoding();
      Codec codec = codecFactory.create_codec(encode);
      int slotId = info.allocate_slot_id();
      info.add_server_request_interceptor(new ServerInterceptor(codec, slotId));

      switch (policy) {
        case CredentialValidationPolicy.ALWAYS:
          info.add_server_request_interceptor(new CredentialValidatorServerInterceptor());
          break;

        case CredentialValidationPolicy.CACHED:
          info.add_server_request_interceptor(new CachedCredentialValidatorServerInterceptor());
          break;

        case CredentialValidationPolicy.NONE:
          break;
        default:
          logger.Error(
              "Não foi escolhida nenhuma política para a validação de credenciais obtidas pelo interceptador servidor.");
          break;
      }
      logger.Info("O interceptador servidor foi registrado.");
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
