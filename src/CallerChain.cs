using tecgraf.openbus.core.v2_0.services.access_control;

namespace tecgraf.openbus {

  /// <summary>
  /// Cadeia de chamadas oriundas de um barramento.
  /// 
  /// Coleção de informações dos logins que originaram chamadas em cadeia através
  /// de um barramento. Cadeias de chamadas representam chamadas aninhadas dentro
  /// do barramento e são úteis para que os sistemas que recebam essas chamadas
  /// possam identificar se a chamada foi originada por entidades autorizadas ou
  /// não.
  /// </summary>
  public interface CallerChain {

    /// <summary>
    /// Barramento através do qual essas chamadas foram originadas.
    /// </summary>
    string BusId { get; }

    /// <summary>
    /// Login para o qual a chamada estava destinada. Só é possível fazer chamadas
    /// dentro dessa cadeia (através do método joinChain da interface 
    /// OpenBusContext) se o login da conexão corrente for o mesmo do target.
    ///
    /// No caso de conexões legadas, este campo será nulo e será possível fazer
    /// qualquer chamada como parte dessa cadeia. Contudo, todas as chamadas
    /// feitas como parte de uma cadeia de uma chamada legada serão feitas
    /// utilizando apenas o protocolo do OpenBus 1.5 (apenas com credenciais
    /// legadas) e portanto serão recusadas por serviços que não aceitem chamadas
    /// legadas (OpenBus 1.5).
    /// </summary>
    LoginInfo? Target { get; }

    /// <summary>
    /// Lista de informações de login de todas as entidades que originaram as
    /// chamadas nessa cadeia. Quando essa lista é vazia isso indica que a
    /// chamada não está inclusa em outra cadeia de chamadas.
    /// </summary>
    LoginInfo[] Originators { get; }

    /// <summary>
	  /// Informação de login da entidade que realizou a última chamada da cadeia.
    /// </summary>
    LoginInfo Caller { get; }
  }
}
