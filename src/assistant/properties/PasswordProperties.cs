namespace tecgraf.openbus.assistant.properties {
  /// <summary>
  /// 
  /// </summary>
  public class PasswordProperties : AssistantPropertiesImpl {
    /// <summary>
    /// 
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="password"></param>
    public PasswordProperties(string entity, byte[] password) {
      Entity = entity;
      Password = password;
    }

    /// <summary>
    /// 
    /// </summary>
    public byte[] Password { get; private set; }

    /// <summary>
    /// 
    /// </summary>
    public string Entity { get; private set; }
  }
}
