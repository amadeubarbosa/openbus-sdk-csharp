using tecgraf.openbus.core.v2_0.services.access_control;

namespace tecgraf.openbus.Test {
  class InvalidLoginCallbackMock : InvalidLoginCallback {
    private readonly string _login;
    private readonly byte[] _password;

    internal InvalidLoginCallbackMock(string login, byte[] password) {
      _login = login;
      _password = password;
    }

    public void InvalidLogin(Connection conn, LoginInfo login, string busId) {
      ConnectionTest.CallbackCalled = true;
      conn.LoginByPassword(_login, _password);
    }
  }
}
