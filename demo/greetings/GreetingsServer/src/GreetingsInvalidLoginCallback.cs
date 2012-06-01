using scs.core;
using tecgraf.openbus;
using tecgraf.openbus.core.v2_00.services.offer_registry;

namespace greetings {
  class GreetingsInvalidLoginCallback : InvalidLoginCallback {
    private readonly string _entity;
    private readonly byte[] _privKey;
    private readonly IComponent[] _components;
    private readonly ServiceProperty[] _properties;

    public GreetingsInvalidLoginCallback(string entity, byte[] privKey, IComponent[] components, ServiceProperty[] properties) {
      _entity = entity;
      _privKey = privKey;
      _components = components;
      _properties = properties;
    }

    public bool InvalidLogin(Connection conn) {
      return GreetingsServer.Login(_entity, _privKey) && GreetingsServer.Register(_components, _properties);
    }
  }
}
