using System;
using omg.org.IOP;


namespace tecgraf.openbus.sdk.interceptors
{
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

    /// <summary>
    /// Fornece o objeto responsável pelo marshall/unmarshall de credenciais 
    /// para transporte/obtenção de contextos de requisições de servico.
    /// </summary>
    internal Codec Codec { get; set; }

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
