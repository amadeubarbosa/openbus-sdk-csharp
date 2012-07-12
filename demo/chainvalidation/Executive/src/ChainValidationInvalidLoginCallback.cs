using scs.core;
using tecgraf.openbus;
using tecgraf.openbus.core.v2_0.services.access_control;
using tecgraf.openbus.core.v2_0.services.offer_registry;

namespace chainvalidation {
  internal class ChainValidationInvalidLoginCallback : InvalidLoginCallback {
    private readonly string _entity;
    private readonly byte[] _privKey;
    private readonly IComponent _ic;
    private readonly ServiceProperty[] _properties;

    public ChainValidationInvalidLoginCallback(string entity, byte[] privKey,
                                               IComponent ic,
                                               ServiceProperty[] properties) {
      _entity = entity;
      _privKey = privKey;
      _ic = ic;
      _properties = properties;
    }

    public void InvalidLogin(Connection conn, LoginInfo login, string busId) {
      if (ExecutiveServer.Login(_entity, _privKey)) {
        ExecutiveServer.Register(_ic, _properties);
      }
    }
  }
}