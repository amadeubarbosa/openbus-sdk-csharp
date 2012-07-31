using System.Runtime.Remoting.Channels;
using Ch.Elca.Iiop;
using omg.org.CORBA;
using tecgraf.openbus.interceptors;

namespace tecgraf.openbus {
  /// <summary>
  /// Inicializador de ORBs para acesso a barramentos OpenBus.
  ///
  /// Esse objeto � utilizado para oten��o de ORBs CORBA a ser utilizados
  /// exclusimamente para chamadas atrav�s de barramentos OpenBus.
  /// 
  /// Na vers�o atual do IIOP.Net, a implementa��o do ORB � um singleton e,
  /// portanto, h� sempre apenas uma inst�ncia de ORB. Por isso, h� sempre
  /// tamb�m apenas uma inst�ncia de ConnectionManager.
  /// 
  /// O objetivo original dessa classe seria fornecer um m�todo "InitORB". Como
  /// o ORB � um singleton, ele � automaticamente inicializado durante a
  /// primeira obten��o do ConnectionManager e assim o m�todo "InitORB" n�o �
  /// p�blico.
  /// </summary>
  public static class ORBInitializer {
    #region Fields

    private static readonly OrbServices ORB = OrbServices.GetSingleton();
    private static volatile bool _initialized;
    private static ConnectionManagerImpl _manager;

    #endregion

    #region Public Members

    /// <summary>
    /// Devolve o gerenciador de conex�es.
    /// 
    /// Na vers�o atual do IIOP.Net, a implementa��o do ORB � um singleton e,
    /// portanto, h� sempre apenas uma inst�ncia de ORB. Por isso, h� sempre
    /// tamb�m apenas uma inst�ncia de ConnectionManager.
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