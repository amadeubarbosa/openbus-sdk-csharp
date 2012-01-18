namespace tecgraf.openbus.sdk {
  public interface IExpiredLoginCallback {
    void OnInvalidLogin(IConnection conn);
  }
}
