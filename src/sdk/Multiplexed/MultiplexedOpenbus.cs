using System.Runtime.Remoting.Channels;
using Ch.Elca.Iiop;
using log4net;
using omg.org.CORBA;
using tecgraf.openbus.sdk.interceptors;

namespace tecgraf.openbus.sdk.multiplexed {
  /// <summary>
  /// API de acesso a um barramento OpenBus.
  /// </summary>
  public sealed class MultiplexedOpenBus : OpenBus {
    #region Fields

    private static readonly ILog Logger =
      LogManager.GetLogger(typeof(MultiplexedOpenBus));

    private static MultiplexedOpenBus _instance;

    /// <summary>
    /// O Orb do IIOP.NET.
    /// <!-- Atenção: O orb é singleton, com isso só podemos registrar 
    /// interceptadores uma única vez. -->
    /// </summary>
    private readonly OrbServices _orb = OrbServices.GetSingleton();

    #endregion

    #region Constructors

    /// <summary>
    /// Construtor da classe MultiplexedOpenBus
    /// </summary>
    private MultiplexedOpenBus() {
      InitORB();
    }

    #endregion

    #region Public Members

    public static OpenBus Instance {
      get { return _instance ?? (_instance = new MultiplexedOpenBus()); }
    }

    public ConnectionMultiplexer Multiplexer { get; private set; }

    /// <summary>
    /// Cria uma conexão com um barramento. Somente uma conexão é possível na API padrão, sem multiplexação.
    /// <returns>A conexão.</returns>
    /// </summary>
    public Connection Connect(string host, short port) {
      //TODO: adicionar conexão
      return new ConnectionImpl(host, port);
    }

    #endregion

    private void InitORB() {
      // Adiciona interceptadores
      InterceptorsInitializer initializer = new InterceptorsInitializer(true);
      Multiplexer = initializer.Multiplexer;
      _orb.RegisterPortableInterceptorInitalizer(initializer);
      _orb.CompleteInterceptorRegistration();

      ChannelServices.RegisterChannel(new IiopChannel(0), false);
    }
  }
}