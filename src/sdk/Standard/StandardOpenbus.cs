using System.Runtime.Remoting.Channels;
using Ch.Elca.Iiop;
using log4net;
using omg.org.CORBA;
using tecgraf.openbus.sdk.Standard.Interceptors;
using tecgraf.openbus.sdk.exceptions;

namespace tecgraf.openbus.sdk.Standard {
  /// <summary>
  /// API de acesso a um barramento OpenBus.
  /// </summary>
  public class StandardOpenbus : OpenBus {
    #region Fields

    private static readonly ILog Logger =
      LogManager.GetLogger(typeof (StandardOpenbus));

    private static StandardOpenbus _instance;

    /// <summary>
    /// O Orb do IIOP.NET.
    /// <!-- Atenção: O orb é singleton, com isso só podemos registrar 
    /// interceptadores uma única vez. -->
    /// </summary>
    private readonly OrbServices _orb = OrbServices.GetSingleton();

    #endregion

    #region Constructors

    /// <summary>
    /// Construtor da classe StandardOpenbus
    /// </summary>
    private StandardOpenbus() {
      InitORB();
    }

    #endregion

    #region Public Members

    public static OpenBus Instance {
      get { return _instance ?? (_instance = new StandardOpenbus()); }
    }

    /// <summary>
    /// Cria uma conexão com um barramento. Somente uma conexão é possível na API padrão, sem multiplexação.
    /// <returns>A conexão.</returns>
    /// </summary>
    public Connection Connect(string host, short port) {
      if ((StandardServerInterceptor.Instance.Connection == null) ||
          (StandardClientInterceptor.Instance.Connection == null)) {
        StandardConnection conn = new StandardConnection(host, port);
        StandardServerInterceptor.Instance.Connection = conn;
        StandardClientInterceptor.Instance.Connection = conn;
        return conn;
      }
      throw new AlreadyConnectedException(
        "Utilizando o SDK sem suporte a multiplexação, só pode haver uma conexão com um barramento. Feche a conexão existente antes de realizar uma nova.");
    }

    #endregion

    internal static void RemoveConnection() {
      StandardServerInterceptor.Instance.Connection = null;
      StandardClientInterceptor.Instance.Connection = null;
    }

    private void InitORB() {
      // Adiciona interceptadores
      _orb.RegisterPortableInterceptorInitalizer(new StandardClientInitializer());
      _orb.RegisterPortableInterceptorInitalizer(new StandardServerInitializer());
      _orb.CompleteInterceptorRegistration();

      ChannelServices.RegisterChannel(new IiopChannel(0), false);
    }
  }
}