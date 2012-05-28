namespace tecgraf.openbus.demo.chainvalidation {
  class ChainValidationInvalidLoginCallback : InvalidLoginCallback {
    private readonly string _entity;
    private readonly byte[] _privKey;
    private readonly string _executiveEntity;

    public ChainValidationInvalidLoginCallback(string entity, byte[] privKey, string executiveEntity) {
      _entity = entity;
      _privKey = privKey;
      _executiveEntity = executiveEntity;
    }

    public bool InvalidLogin(Connection conn) {
      return SecretaryServer.Login(_entity, _privKey) && SecretaryServer.Register(_executiveEntity);
    }
  }
}
