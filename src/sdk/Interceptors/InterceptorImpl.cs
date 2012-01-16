using System;
using omg.org.IOP;


namespace Tecgraf.Openbus.Interceptors
{
  /// <summary>
  /// Implementa um interceptador.
  /// </summary>
  internal class InterceptorImpl : omg.org.PortableInterceptor.Interceptor
  {

    #region Fields

    /// <summary>
    /// Representa a identificação do "service context" (contexto) utilizado
    /// para transporte de credenciais em requisições de serviço.
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
    /// Fornece o objeto responsável pelo marshall/unmarshall de credenciais 
    /// para transporte/obtenção de contextos de requisições de servico.
    /// </summary>
    public Codec Codec {
      get { return codec; }
    }
    private Codec codec;

    #endregion

    #region Contructor

    /// <summary>
    /// Inicializa uma nova instância de OpenbusAPI.Interceptors.InterceptorImpl
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
