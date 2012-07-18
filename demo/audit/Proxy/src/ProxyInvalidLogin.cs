using tecgraf.openbus;
using tecgraf.openbus.core.v2_0.services.access_control;

namespace audit {
  internal class ProxyInvalidLogin : InvalidLoginCallback {
    private readonly string _entity;
    private readonly byte[] _privKey;
    private readonly string _serverEntity;

    internal ProxyInvalidLogin(string entity, byte[] privKey,
                               string serverEntity) {
      _entity = entity;
      _privKey = privKey;
      _serverEntity = serverEntity;
    }

    public void InvalidLogin(Connection conn, LoginInfo login) {
      if (AuditProxy.Login(_entity, _privKey)) {
        AuditProxy.Register(_serverEntity);
      }
    }
  }
}