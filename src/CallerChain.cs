using tecgraf.openbus.core.v2_00.services.access_control;

namespace tecgraf.openbus {
  
  public interface CallerChain {

    /// <summary>
    /// Barramento através do qual as chamadas foram originadas.
    /// </summary>
    string BusId { get; }

    /// <summary>
    /// Lista de informações de login de todas as entidades que realizaram chamadas
    /// que originaram a cadeia de chamadas da qual essa chamada está inclusa.
    /// Quando essa lista é vazia isso indica que a chamada não está inclusa numa
    /// cadeia de chamadas.
    /// </summary>
    LoginInfo[] Originators { get; }

    /// <summary>
    /// Informação de login da entidade que iniciou a chamada
    /// </summary>
    LoginInfo Caller { get; }
  }
}
