using scs.core;
using tecgraf.openbus;
using tecgraf.openbus.core.v2_00.services.offer_registry;

namespace Server {
  internal class IndependentClockInvalidLoginCallback : InvalidLoginCallback {
    private readonly IComponent _ic;
    private readonly ServiceProperty[] _properties;

    public IndependentClockInvalidLoginCallback(IComponent ic,
                                                ServiceProperty[] properties) {
      _ic = ic;
      _properties = properties;
    }

    public bool InvalidLogin(Connection conn) {
      return IndependentClockServer.TryLoginAndRegisterForever(_ic, _properties);
    }
  }
}