using tecgraf.openbus.core.v2_0.services.access_control;

namespace tecgraf.openbus.assistant.properties {
  /// <summary>
  /// 
  /// </summary>
  /// <param name="secret"></param>
  /// <returns></returns>
  public delegate LoginProcess SharedAuthHandler(out byte[] secret);

  /// <summary>
  /// 
  /// </summary>
  public class SharedAuthProperties : AssistantPropertiesImpl {
    /// <summary>
    /// 
    /// </summary>
    /// <param name="callback"></param>
    public SharedAuthProperties(SharedAuthHandler callback) {
      Callback = callback;
      Type = LoginType.SharedAuth;
    }

    /// <summary>
    /// 
    /// </summary>
    public SharedAuthHandler Callback { get; private set; }
  }
}
