using tecgraf.openbus;
using tecgraf.openbus.core.v2_0.services.access_control;

namespace chainvalidation {
  class ChainValidationInvalidLoginCallback : InvalidLoginCallback {
    private readonly string _entity;
    private readonly byte[] _privKey;
    private readonly string _executiveEntity;

    public ChainValidationInvalidLoginCallback(string entity, byte[] privKey, string executiveEntity) {
      _entity = entity;
      _privKey = privKey;
      _executiveEntity = executiveEntity;
    }

    public void InvalidLogin(Connection conn, LoginInfo login, string busId) {
      if (SecretaryServer.Login(_entity, _privKey)) {
        SecretaryServer.Register(_executiveEntity);
      }
    }
  }
}
