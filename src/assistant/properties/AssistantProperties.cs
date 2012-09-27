using omg.org.CORBA;

namespace tecgraf.openbus.assistant.properties {
  /// <summary>
  /// 
  /// </summary>
  public interface AssistantProperties {
    /// <summary>
    /// 
    /// </summary>
    int Interval { get; set; }

    /// <summary>
    /// 
    /// </summary>
    //TODO manter ORB aqui já que é singleton? sou inclinado a remover pois não vejo isso mudando tão cedo e precisará mudar em outros pontos de qq forma.
    ORB ORB { get; }

    /// <summary>
    /// 
    /// </summary>
    ConnectionProperties Props { get; set; }

    /// <summary>
    /// 
    /// </summary>
    OnFailureCallback FailureCallback { get; set; }
  }
}
