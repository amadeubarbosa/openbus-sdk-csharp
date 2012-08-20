using System.Runtime.Remoting.Channels;
using Ch.Elca.Iiop;
using omg.org.CORBA;
using tecgraf.openbus.interceptors;

namespace tecgraf.openbus {
  /// <summary>
  /// Inicializador de ORBs para acesso a barramentos OpenBus.
  ///
  /// Esse objeto é utilizado para a obtenção de ORBs CORBA a serem utilizados
  /// exclusivamente para chamadas através de barramentos OpenBus.
  /// 
  /// Na versão atual do IIOP.Net, a implementação do ORB é um singleton e,
  /// portanto, há sempre apenas uma instância de ORB. Por isso, há sempre
  /// também apenas uma instância de CallContext.
  /// 
  /// O objetivo original dessa classe seria fornecer um método "InitORB". Como
  /// o ORB é um singleton, ele é automaticamente inicializado durante a
  /// primeira obtenção do CallContext e assim o método "InitORB" não é
  /// público.
  /// </summary>
  public static class ORBInitializer {
    #region Fields

    private static readonly OrbServices ORB = OrbServices.GetSingleton();
    private static volatile bool _initialized;
    private static CallContextImpl _context;

    #endregion

    #region Public Members

    /// <summary>
    /// Devolve o gerenciador de conexões.
    /// 
    /// Na versão atual do IIOP.Net, a implementação do ORB é um singleton e,
    /// portanto, há sempre apenas uma instância de ORB. Por isso, há sempre
    /// também apenas uma instância de CallContext.
    /// </summary>
    public static CallContext Context { 
      get {
        if (!_initialized) {
          InitORB();
        }
        return _context;
      } 
      private set {
        _context = value as CallContextImpl;
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
        Context = initializer.Context;
        _initialized = true;
      }
      return ORB;
    }
  }
}