using System;
using System.Security.Cryptography;
using System.Text;
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

    //TODO: Maia vai criar constantes na IDL para os 3 casos abaixo
    private const byte MajorVersion = core.v2_00.MajorVersion.ConstVal;
    private const byte MinorVersion = core.v2_00.MinorVersion.ConstVal;
//    protected readonly const int SecretSize = 16;

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

    #endregion

    protected byte[] CreateCredentialHash(string operation, int ticket,
                                    byte[] secret, int requestId) {
      UTF8Encoding utf8 = new UTF8Encoding();
      // 2 bytes para versao, 16 para o segredo, 4 para o ticket em little endian, 4 para o request id em little endian e X para a operacao.
      int size = 2 + secret.Length + 4 + 4 + utf8.GetByteCount(operation);
      byte[] hash = new byte[size];
      hash[0] = MajorVersion;
      hash[1] = MinorVersion;
      int index = 2;
      secret.CopyTo(hash, index);
      byte[] bTicket = BitConverter.GetBytes(ticket);
      byte[] bRequestId = BitConverter.GetBytes(requestId);
      if (!BitConverter.IsLittleEndian) {
        Array.Reverse(bTicket);
        Array.Reverse(bRequestId);
      }
      index += secret.Length;
      bTicket.CopyTo(hash, index);
      index += 4;
      bRequestId.CopyTo(hash, index);
      byte[] bOperation = utf8.GetBytes(operation);
      index += 4;
      bOperation.CopyTo(hash, index);
      return SHA256.Create().ComputeHash(hash);
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
  }
}
