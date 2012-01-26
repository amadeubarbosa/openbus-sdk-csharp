using log4net;
using omg.org.IOP;

namespace tecgraf.openbus.sdk.Standard.Interceptors
{
  /// <summary>
  /// Classe responsável por inicializar o interceptador cliente.
  /// Implementa ProtableInterceptor.ORBInitializer.
  /// </summary>
  internal class StandardClientInitializer : omg.org.PortableInterceptor.ORBInitializer
  {
    #region Field

    private static readonly ILog Logger = LogManager.GetLogger(typeof(StandardClientInitializer));

    private readonly StandardOpenbus _bus;

    #endregion

    #region Constructor

    /// <summary>
    /// Construtor.
    /// <param name="bus">O barramento sendo utilizado. Esse é um barramento de apenas uma conexão.</param>
    /// </summary>
    public StandardClientInitializer(StandardOpenbus bus) {
      _bus = bus;
    }

    #endregion

    #region ORBInitializer Members

    /// <inheritdoc />
    public void post_init(omg.org.PortableInterceptor.ORBInitInfo info) {
      try {
        Encoding encode = new Encoding(ENCODING_CDR_ENCAPS.ConstVal, 1, 2);
        Codec codec = info.codec_factory.create_codec(encode);
        info.add_client_request_interceptor(new StandardClientInterceptor(_bus, codec));

        Logger.Info("Interceptador cliente registrado.");
      }
      catch (System.Exception e) {
        Logger.Error("Erro no registro do interceptador cliente", e);
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
