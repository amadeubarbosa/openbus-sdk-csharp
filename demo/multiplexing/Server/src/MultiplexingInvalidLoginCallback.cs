using scs.core;
using tecgraf.openbus;
using tecgraf.openbus.core.v2_0.services.access_control;
using tecgraf.openbus.core.v2_0.services.offer_registry;

namespace multiplexing {
  internal class MultiplexingInvalidLoginCallback : InvalidLoginCallback {
    private readonly string _entity;
    private readonly byte[] _privKey;
    private readonly IComponent _component;
    private readonly ServiceProperty[] _properties;

    public MultiplexingInvalidLoginCallback(string entity, byte[] privKey,
                                            IComponent component,
                                            ServiceProperty[] properties) {
      _entity = entity;
      _privKey = privKey;
      _component = component;
      _properties = properties;
    }

    public void InvalidLogin(Connection conn, LoginInfo login, string busId) {
      if (MultiplexingServer.Login(conn, _entity, _privKey)) {
        MultiplexingServer.Register(conn, _component, _properties);
      }
    }
  }
}