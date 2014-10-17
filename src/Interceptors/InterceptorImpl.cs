using System;
using tecgraf.openbus.core.v2_1.credential;

namespace tecgraf.openbus.interceptors {
  /// <summary>
  /// Implementa um interceptador.
  /// </summary>
  internal class InterceptorImpl : omg.org.PortableInterceptor.Interceptor {
    #region Fields

    /// <summary>
    /// Fornece o nome do interceptador.
    /// </summary>
    public string Name {
      get { return _name; }
    }

    private readonly String _name = String.Empty;

    internal OpenBusContextImpl Context;

    /// <summary>
    /// Representam a identificação dos "service contexts" (contextos) utilizados
    /// para transporte de credenciais em requisições de serviço.
    /// </summary>
    protected const int ContextId = CredentialContextId.ConstVal;

    #endregion

    #region Contructor

    /// <summary>
    /// Inicializa uma nova instância de OpenbusAPI.Interceptors.InterceptorImpl
    /// </summary>
    /// <param name="name">O nome do interceptador</param>
    protected InterceptorImpl(String name) {
      _name = name;
    }

    #endregion
  }
}