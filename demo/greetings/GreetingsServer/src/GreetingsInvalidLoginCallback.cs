using scs.core;
using tecgraf.openbus;
using tecgraf.openbus.core.v2_0.services.access_control;
using tecgraf.openbus.core.v2_0.services.offer_registry;

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

    public void InvalidLogin(Connection conn, LoginInfo login, string busId) {
      if (GreetingsServer.Login(_entity, _privKey)) {
        GreetingsServer.Register(_components, _properties);
      }
    }
  }
}
