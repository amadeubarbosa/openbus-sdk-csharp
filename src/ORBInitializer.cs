using System.Collections;
using omg.org.CORBA;
using tecgraf.openbus.exceptions;
using tecgraf.openbus.interceptors;

namespace tecgraf.openbus {
  /// <summary>
  /// Inicializador de ORBs para acesso a barramentos OpenBus.
  ///
  /// Esse objeto é utilizado para a obtenção de ORBs CORBA a serem utilizados
  /// exclusivamente para chamadas através de barramentos OpenBus.
  /// 
  /// Na versão atual do IIOP.Net a implementação do ORB é um singleton e,
  /// portanto, há sempre apenas uma instância de ORB. Por isso, há sempre
  /// também apenas uma instância de OpenBusContext.
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
    /// Fornece o gerenciador de conexões.
    /// 
    /// Na versão atual do IIOP.Net a implementação do ORB é um singleton e,
    /// portanto, há sempre apenas uma instância de ORB. Por isso, há sempre
    /// também apenas uma instância de OpenBusContext.
    /// </summary>
    public static OpenBusContext Context { 
      get {
        if (_initialized) {
          return _context;
        }
        throw new ORBNotInitializedException("O ORB deve ser inicializado para o OpenBus primeiro. Use o método InitORB.");
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