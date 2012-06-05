using tecgraf.openbus;

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

    public bool InvalidLogin(Connection conn) {
      return AuditProxy.Login(_entity, _privKey) &&
             AuditProxy.Register(_serverEntity);
    }
  }
}