using scs.core;
using tecgraf.openbus;
using tecgraf.openbus.core.v2_00.services.offer_registry;

namespace Server {
  internal class DedicatedClockInvalidLoginCallback : InvalidLoginCallback {
    private readonly string _entity;
    private readonly byte[] _privKey;
    private readonly IComponent _ic;
    private readonly ServiceProperty[] _properties;
    private readonly int _waitTime;

    public DedicatedClockInvalidLoginCallback(string entity, byte[] privKey,
                                              IComponent ic,
                                              ServiceProperty[] properties,
                                              int waitTime) {
      _entity = entity;
      _privKey = privKey;
      _ic = ic;
      _properties = properties;
      _waitTime = waitTime;
    }

    public bool InvalidLogin(Connection conn) {
      return DedicatedClockServer.TryLoginAndRegisterForever(_entity, _privKey,
                                                             _ic, _properties,
                                                             _waitTime);
    }
  }
}