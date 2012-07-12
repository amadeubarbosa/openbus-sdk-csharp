using tecgraf.openbus;
using tecgraf.openbus.core.v2_0.services.access_control;

namespace audit {
  internal class ProxyInvalidLogin : InvalidLoginCallback {
    private readonly string _entity;
    private readonly byte[] _privKey;
    private readonly string _serverEntity;

    public ProxyInvalidLogin(string entity, byte[] privKey, string serverEntity) {
      _entity = entity;
      _privKey = privKey;
      _serverEntity = serverEntity;
    }

    public void InvalidLogin(Connection conn, LoginInfo login, string busId) {
      if (AuditProxy.Login(_entity, _privKey)) {
        AuditProxy.Register(_serverEntity);
      }
    }
  }
}