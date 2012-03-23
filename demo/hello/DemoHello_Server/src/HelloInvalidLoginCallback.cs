using tecgraf.openbus.sdk;

namespace tecgraf.openbus.demo.hello {
  class HelloInvalidLoginCallback : InvalidLoginCallback {
    private readonly string _entity;
    private readonly byte[] _privKey;

    public HelloInvalidLoginCallback(string entity, byte[] privKey) {
      _entity = entity;
      _privKey = privKey;
    }

    public bool InvalidLogin(Connection conn) {
      conn.LoginByCertificate(_entity, _privKey);
      return conn.Login != null;
    }
  }
}
