using scs.core;
using tecgraf.openbus;
using tecgraf.openbus.core.v2_0.services.access_control;
using tecgraf.openbus.core.v2_0.services.offer_registry;

namespace Server {
  internal class IndependentClockInvalidLoginCallback : InvalidLoginCallback {
    private readonly IComponent _ic;
    private readonly ServiceProperty[] _properties;

    internal IndependentClockInvalidLoginCallback(IComponent ic,
                                                  ServiceProperty[] properties) {
      _ic = ic;
      _properties = properties;
    }

    public void InvalidLogin(Connection conn, LoginInfo login) {
      IndependentClockServer.TryLoginAndRegisterForever(_ic, _properties);
    }
  }
}