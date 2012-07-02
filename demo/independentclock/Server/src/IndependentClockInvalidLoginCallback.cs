using scs.core;
using tecgraf.openbus;
using tecgraf.openbus.core.v2_0.services.access_control;
using tecgraf.openbus.core.v2_0.services.offer_registry;

namespace Server {
  internal class IndependentClockInvalidLoginCallback : InvalidLoginCallback {
    private readonly IComponent _ic;
    private readonly ServiceProperty[] _properties;

    public IndependentClockInvalidLoginCallback(IComponent ic,
                                                ServiceProperty[] properties) {
      _ic = ic;
      _properties = properties;
    }

    public bool InvalidLogin(Connection conn, LoginInfo login, string busId) {
      return IndependentClockServer.TryLoginAndRegisterForever(_ic, _properties);
    }
  }
}