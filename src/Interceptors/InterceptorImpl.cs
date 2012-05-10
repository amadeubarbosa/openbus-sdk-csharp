using System;
using omg.org.IOP;
using tecgraf.openbus.core.v2_00.credential;

namespace tecgraf.openbus.interceptors
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
    /// Fornece o objeto respons�vel pelo marshall/unmarshall de credenciais 
    /// para transporte/obten��o de contextos de requisi��es de servico.
    /// </summary>
    internal Codec Codec { get; set; }

    internal int CredentialSlotId;
    internal int ConnectionSlotId;

    internal ConnectionManagerImpl Manager;

    internal bool Legacy;

    /// <summary>
    /// Representam a identifica��o dos "service contexts" (contextos) utilizados
    /// para transporte de credenciais em requisi��es de servi�o.
    /// </summary>
    protected const int ContextId = CredentialContextId.ConstVal;

    protected const int PrevContextId = 1234;

    #endregion

    #region Contructor

    /// <summary>
    /// Inicializa uma nova inst�ncia de OpenbusAPI.Interceptors.InterceptorImpl
    /// </summary>
    /// <param name="name">O nome do interceptador</param>
    protected InterceptorImpl(String name) {
      _name = name;
    }

    #endregion
  }
}
