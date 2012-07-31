using System.Runtime.Remoting.Channels;
using Ch.Elca.Iiop;
using omg.org.CORBA;
using tecgraf.openbus.interceptors;

namespace tecgraf.openbus {
  /// <summary>
  /// Inicializador de ORBs para acesso a barramentos OpenBus.
  ///
  /// Esse objeto é utilizado para otenção de ORBs CORBA a ser utilizados
  /// exclusimamente para chamadas através de barramentos OpenBus.
  /// 
  /// Na versão atual do IIOP.Net, a implementação do ORB é um singleton e,
  /// portanto, há sempre apenas uma instância de ORB. Por isso, há sempre
  /// também apenas uma instância de ConnectionManager.
  /// 
  /// O objetivo original dessa classe seria fornecer um método "InitORB". Como
  /// o ORB é um singleton, ele é automaticamente inicializado durante a
  /// primeira obtenção do ConnectionManager e assim o método "InitORB" não é
  /// público.
  /// </summary>
  public static class ORBInitializer {
    #region Fields

    private static readonly OrbServices ORB = OrbServices.GetSingleton();
    private static volatile bool _initialized;
    private static ConnectionManagerImpl _manager;

    #endregion

    #region Public Members

    /// <summary>
    /// Devolve o gerenciador de conexões.
    /// 
    /// Na versão atual do IIOP.Net, a implementação do ORB é um singleton e,
    /// portanto, há sempre apenas uma instância de ORB. Por isso, há sempre
    /// também apenas uma instância de ConnectionManager.
    /// </summary>
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