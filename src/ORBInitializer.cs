using System.Collections;
using omg.org.CORBA;
using tecgraf.openbus.exceptions;
using tecgraf.openbus.interceptors;

namespace tecgraf.openbus {
  /// <summary>
  /// Inicializador de ORBs para acesso a barramentos OpenBus.
  ///
  /// Esse objeto � utilizado para a obten��o de ORBs CORBA a serem utilizados
  /// exclusivamente para chamadas atrav�s de barramentos OpenBus.
  /// 
  /// Na vers�o atual do IIOP.Net a implementa��o do ORB � um singleton e,
  /// portanto, h� sempre apenas uma inst�ncia de ORB. Por isso, h� sempre
  /// tamb�m apenas uma inst�ncia de OpenBusContext.
  /// </summary>
  public static class ORBInitializer {
    #region Fields

    private static readonly OrbServices ORB = OrbServices.GetSingleton();
    private static volatile bool _initialized;
    private static OpenBusContextImpl _context;
    private static readonly object Lock = new object();

    #endregion

    #region Public Members

    /// <summary>
    /// Fornece o gerenciador de conex�es.
    /// 
    /// Na vers�o atual do IIOP.Net a implementa��o do ORB � um singleton e,
    /// portanto, h� sempre apenas uma inst�ncia de ORB. Por isso, h� sempre
    /// tamb�m apenas uma inst�ncia de OpenBusContext.
    /// </summary>
    public static OpenBusContext Context { 
      get {
        if (_initialized) {
          return _context;
        }
        throw new ORBNotInitializedException("O ORB deve ser inicializado para o OpenBus primeiro. Use o m�todo InitORB.");
      } 
      private set {
        _context = value as OpenBusContextImpl;
      } 
    }

    /// <summary>
    /// Inicializa o ORB, transformando-o em um ORB preparado para o OpenBus.
    /// </summary>
    /// <param name="properties">Conjunto opcional de propriedades a ser passada para o canal IIOP do servidor.</param>
    /// <returns>O ORB.</returns>
    public static OrbServices InitORB(IDictionary properties = null) {
      lock (Lock) {
        if (!_initialized) {
          // Adiciona interceptadores
          InterceptorsInitializer initializer = new InterceptorsInitializer();
          ORB.RegisterPortableInterceptorInitalizer(initializer);
          ORB.CompleteInterceptorRegistration();
          if (properties != null){
            OrbServices.CreateAndRegisterIiopChannel(properties);
          }
          else{
            OrbServices.CreateAndRegisterIiopChannel(0);
          }
          Context = initializer.Context;
          _initialized = true;
        }
      }
      return ORB;
    }

    #endregion
  }
}