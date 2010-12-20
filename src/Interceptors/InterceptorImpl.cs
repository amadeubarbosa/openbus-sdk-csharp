using System;
using omg.org.IOP;


namespace OpenbusAPI.Interceptors
{
  /// <summary>
  /// Implementa um interceptador.
  /// </summary>
  internal class InterceptorImpl : omg.org.PortableInterceptor.Interceptor
  {

    #region Fields

    /// <summary>
    /// Representa a identifica��o do "service context" (contexto) utilizado
    /// para transporte de credenciais em requisi��es de servi�o.
    /// </summary>
    internal protected readonly int CONTEXT_ID = 1234;

    /// <summary>
    /// Fornece o nome do interceptador.
    /// </summary>
    public string Name {
      get { return name; }
    }
    private String name = String.Empty;

    /// <summary>
    /// Fornece o objeto respons�vel pelo marshall/unmarshall de credenciais 
    /// para transporte/obten��o de contextos de requisi��es de servico.
    /// </summary>
    public Codec Codec {
      get { return codec; }
    }
    private Codec codec;

    #endregion

    #region Contructor

    /// <summary>
    /// Inicializa uma nova inst�ncia de OpenbusAPI.Interceptors.InterceptorImpl
    /// </summary>
    /// <param name="name">O nome do interceptador</param>
    /// <param name="codec">Elemento codificador/decodificador</param>
    public InterceptorImpl(String name, Codec codec) {
      this.name = name;
      this.codec = codec;
    }

    #endregion
  }
}
