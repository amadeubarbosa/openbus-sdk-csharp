using System.Runtime.Remoting.Channels;
using Ch.Elca.Iiop;
using log4net;
using omg.org.CORBA;
using tecgraf.openbus.sdk.exceptions;
using tecgraf.openbus.sdk.standard.interceptors;

namespace tecgraf.openbus.sdk.standard {
  /// <summary>
  /// API de acesso a um barramento OpenBus.
  /// </summary>
  public sealed class StandardOpenBus : OpenBus {
    #region Fields

    private static readonly ILog Logger =
      LogManager.GetLogger(typeof (StandardOpenBus));

    private static StandardOpenBus _instance;

    /// <summary>
    /// O Orb do IIOP.NET.
    /// <!-- Atenção: O orb é singleton, com isso só podemos registrar 
    /// interceptadores uma única vez. -->
    /// </summary>
    private readonly OrbServices _orb = OrbServices.GetSingleton();

    #endregion

    #region Constructors

    /// <summary>
    /// Construtor da classe StandardOpenBus
    /// </summary>
    private StandardOpenBus() {
      InitORB();
    }

    #endregion

    #region Public Members

    public static OpenBus Instance {
      get { return _instance ?? (_instance = new StandardOpenBus()); }
    }

    /// <summary>
    /// Cria uma conexão com um barramento. Somente uma conexão é possível na API padrão, sem multiplexação.
    /// <returns>A conexão.</returns>
    /// </summary>
    public Connection Connect(string host, short port) {
      if ((StandardServerInterceptor.Instance.Connection == null) &&
          (StandardClientInterceptor.Instance.Connection == null)) {
        return new StandardConnection(host, port);
      }
      throw new AlreadyConnectedException(
        "Utilizando o SDK sem suporte a multiplexação, só pode haver uma conexão com um barramento. Feche a conexão existente antes de realizar uma nova.");
    }

    #endregion

    private void InitORB() {
      // Adiciona interceptadores
      _orb.RegisterPortableInterceptorInitalizer(new InterceptorsInitializer(false));
      _orb.CompleteInterceptorRegistration();

      ChannelServices.RegisterChannel(new IiopChannel(0), false);
    }
  }
}