using scs.core;
using tecgraf.openbus;
using tecgraf.openbus.core.v2_0.services.access_control;
using tecgraf.openbus.core.v2_0.services.offer_registry;

namespace Server {
  internal class DedicatedClockInvalidLoginCallback : InvalidLoginCallback {
    private readonly string _host;
    private readonly short _port;
    private readonly string _entity;
    private readonly byte[] _privKey;
    private readonly IComponent _ic;
    private readonly ServiceProperty[] _properties;
    private readonly int _waitTime;

    public DedicatedClockInvalidLoginCallback(string host, short port,
                                              string entity, byte[] privKey,
                                              IComponent ic,
                                              ServiceProperty[] properties,
                                              int waitTime) {
      _host = host;
      _port = port;
      _entity = entity;
      _privKey = privKey;
      _ic = ic;
      _properties = properties;
      _waitTime = waitTime;
    }

    public void InvalidLogin(Connection conn, LoginInfo login, string busId) {
      DedicatedClockServer.TryLoginAndRegisterForever(_host, _port, _entity,
                                                      _privKey, _ic, _properties,
                                                      _waitTime);
    }
  }
}