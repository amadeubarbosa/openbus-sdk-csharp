using System;
using omg.org.IOP;


namespace tecgraf.openbus.sdk.Interceptors
{
  /// <summary>
  /// Implementa um interceptador.
  /// </summary>
  internal class InterceptorImpl : omg.org.PortableInterceptor.Interceptor
  {

    #region Fields

    /// <summary>
    /// Representam a identificação dos "service contexts" (contextos) utilizados
    /// para transporte de credenciais em requisições de serviço.
    /// </summary>
    protected const int ContextId = core.v2_00.credential.CredentialContextId.ConstVal;
    protected const int PrevContextId = 1234;

    protected readonly byte MajorVersion = Byte.Parse(core.v2_00.Version.ConstVal.Substring(0, 1));
    protected readonly byte MinorVersion = Byte.Parse(core.v2_00.Version.ConstVal.Substring(2, 1));
    protected readonly const int SecretSize = 16;

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
    protected Codec Codec {
      get { return _codec; }
    }
    private readonly Codec _codec;

    #endregion

    #region Contructor

    /// <summary>
    /// Inicializa uma nova instância de OpenbusAPI.Interceptors.InterceptorImpl
    /// </summary>
    /// <param name="name">O nome do interceptador</param>
    /// <param name="codec">Elemento codificador/decodificador</param>
    protected InterceptorImpl(String name, Codec codec) {
      _name = name;
      _codec = codec;
    }

    protected class Session {

      public Session(int id, byte[] secret, string remoteLogin) {
        Id = id;
        Secret = secret;
        RemoteLogin = remoteLogin;
        Ticket = -1;
      }

      public string RemoteLogin { get; private set; }

      public byte[] Secret { get; private set; }

      public int Id { get; private set; }

      public int Ticket { get; private set; }
    }

    #endregion
  }
}
