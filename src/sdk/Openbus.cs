namespace tecgraf.openbus.sdk
{
  /// <summary>
  /// API de acesso a um barramento OpenBus.
  /// </summary>
  public interface OpenBus
  {
    /// <summary>
    /// Cria uma nova conexão com um barramento.
    /// </summary>
    Connection Connect(string host, short port);
  }
}
