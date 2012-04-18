using System.Runtime.Remoting.Channels;
using Ch.Elca.Iiop;
using omg.org.CORBA;
using tecgraf.openbus.sdk.interceptors;

namespace tecgraf.openbus.sdk {
  /// <summary>
  /// API de acesso a um barramento OpenBus.
  /// </summary>
  public static class ORBInitializer {
    #region Fields

    /// <summary>
    /// O Orb do IIOP.NET.
    /// <!-- Atenção: O orb é singleton, com isso só podemos registrar 
    /// interceptadores uma única vez. -->
    /// </summary>
    private static readonly OrbServices ORB = OrbServices.GetSingleton();
    private static bool _initialized;
    private static ConnectionManagerImpl _manager;

    #endregion

    #region Public Members

    public static ConnectionManager Manager { 
      get {
        if (!_initialized) {
          InitORB();
        }
        return _manager;
      } 
      private set {
        _manager = value as ConnectionManagerImpl;
      } 
    }

    #endregion

    private static ORB InitORB() {
      if (!_initialized) {
        // Adiciona interceptadores
        InterceptorsInitializer initializer = new InterceptorsInitializer();
        ORB.RegisterPortableInterceptorInitalizer(initializer);
        ORB.CompleteInterceptorRegistration();
        ChannelServices.RegisterChannel(new IiopChannel(0), false);
        Manager = initializer.Manager;
        _initialized = true;
      }
      return ORB;
    }
  }
}