namespace tecgraf.openbus.sdk {
  interface IExpiredLoginCallback {
    void OnInvalidLogin(IConnection conn);
  }
}
