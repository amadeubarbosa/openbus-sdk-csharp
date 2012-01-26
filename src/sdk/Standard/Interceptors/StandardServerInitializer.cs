using log4net;
using omg.org.IOP;
using tecgraf.openbus.sdk.Interceptors;

namespace tecgraf.openbus.sdk.Standard.Interceptors
{
  internal class StandardServerInitializer : omg.org.PortableInterceptor.ORBInitializer
  {
    #region Fields

    private static readonly ILog Logger = LogManager.GetLogger(typeof(StandardServerInitializer));

    private readonly CredentialValidationPolicy _policy;

    private StandardOpenbus _bus;

    #endregion

    #region Constructor

    /// <summary>
    /// Construtor.
    /// <param name="bus">Barramento de conexão única sendo utilizado.</param>
    /// </summary>
    public StandardServerInitializer(StandardOpenbus bus, CredentialValidationPolicy policy) {
      _policy = policy;
      _bus = bus;
    }

    #endregion

    #region ORBInitializer Members

    /// <inheritdoc />
    public void post_init(omg.org.PortableInterceptor.ORBInitInfo info) {
      CodecFactory codecFactory = info.codec_factory;
      Encoding encode = new Encoding();
      Codec codec = codecFactory.create_codec(encode);
      int slotId = info.allocate_slot_id();
      info.add_server_request_interceptor(new StandardServerInterceptor(codec, slotId));

      switch (_policy) {
        case CredentialValidationPolicy.ALWAYS:
          info.add_server_request_interceptor(new CredentialValidatorServerInterceptor());
          break;

        case CredentialValidationPolicy.CACHED:
          info.add_server_request_interceptor(new CachedCredentialValidatorServerInterceptor());
          break;

        case CredentialValidationPolicy.NONE:
          break;
        default:
          Logger.Error(
              "Não foi escolhida nenhuma política para a validação de credenciais obtidas pelo interceptador servidor.");
          break;
      }
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
