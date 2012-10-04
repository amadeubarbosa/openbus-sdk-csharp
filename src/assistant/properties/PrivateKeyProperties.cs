namespace tecgraf.openbus.assistant.properties {
  /// <summary>
  /// 
  /// </summary>
  public class PrivateKeyProperties : AssistantPropertiesImpl {
    /// <summary>
    /// 
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="privateKey"></param>
    public PrivateKeyProperties(string entity, PrivateKey privateKey) {
      Entity = entity;
      PrivateKey = privateKey;
      Type = LoginType.PrivateKey;
    }

    /// <summary>
    /// 
    /// </summary>
    public PrivateKey PrivateKey { get; private set; }

    /// <summary>
    /// 
    /// </summary>
    public string Entity { get; private set; }
  }
}
