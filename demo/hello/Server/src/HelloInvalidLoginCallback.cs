using scs.core;
using tecgraf.openbus;
using tecgraf.openbus.core.v2_00.services.offer_registry;

namespace hello {
  class HelloInvalidLoginCallback : InvalidLoginCallback {
    private readonly string _entity;
    private readonly byte[] _privKey;
    private readonly IComponent _ic;
    private readonly ServiceProperty[] _properties;

    public HelloInvalidLoginCallback(string entity, byte[] privKey, IComponent ic, ServiceProperty[] properties) {
      _entity = entity;
      _privKey = privKey;
      _ic = ic;
      _properties = properties;
    }

    public bool InvalidLogin(Connection conn) {
      return HelloServer.Login(_entity, _privKey) && HelloServer.Register(_ic, _properties);
    }
  }
}
