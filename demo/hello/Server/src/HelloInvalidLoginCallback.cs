using scs.core;
using tecgraf.openbus;
using tecgraf.openbus.core.v2_0.services.access_control;
using tecgraf.openbus.core.v2_0.services.offer_registry;

namespace hello {
  internal class HelloInvalidLoginCallback : InvalidLoginCallback {
    private readonly string _entity;
    private readonly byte[] _privKey;
    private readonly IComponent _ic;
    private readonly ServiceProperty[] _properties;

    internal HelloInvalidLoginCallback(string entity, byte[] privKey,
                                       IComponent ic,
                                       ServiceProperty[] properties) {
      _entity = entity;
      _privKey = privKey;
      _ic = ic;
      _properties = properties;
    }

    public void InvalidLogin(Connection conn, LoginInfo login, string busId) {
      if (HelloServer.Login(_entity, _privKey)) {
        HelloServer.Register(_ic, _properties);
      }
    }
  }
}