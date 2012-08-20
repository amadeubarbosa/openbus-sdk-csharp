using System.Runtime.Remoting.Channels;
using Ch.Elca.Iiop;
using omg.org.CORBA;
using tecgraf.openbus.interceptors;

namespace tecgraf.openbus {
  /// <summary>
  /// Inicializador de ORBs para acesso a barramentos OpenBus.
  ///
  /// Esse objeto � utilizado para a obten��o de ORBs CORBA a serem utilizados
  /// exclusivamente para chamadas atrav�s de barramentos OpenBus.
  /// 
  /// Na vers�o atual do IIOP.Net, a implementa��o do ORB � um singleton e,
  /// portanto, h� sempre apenas uma inst�ncia de ORB. Por isso, h� sempre
  /// tamb�m apenas uma inst�ncia de CallContext.
  /// 
  /// O objetivo original dessa classe seria fornecer um m�todo "InitORB". Como
  /// o ORB � um singleton, ele � automaticamente inicializado durante a
  /// primeira obten��o do CallContext e assim o m�todo "InitORB" n�o �
  /// p�blico.
  /// </summary>
  public static class ORBInitializer {
    #region Fields

    private static readonly OrbServices ORB = OrbServices.GetSingleton();
    private static volatile bool _initialized;
    private static CallContextImpl _context;

    #endregion

    #region Public Members

    /// <summary>
    /// Devolve o gerenciador de conex�es.
    /// 
    /// Na vers�o atual do IIOP.Net, a implementa��o do ORB � um singleton e,
    /// portanto, h� sempre apenas uma inst�ncia de ORB. Por isso, h� sempre
    /// tamb�m apenas uma inst�ncia de CallContext.
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