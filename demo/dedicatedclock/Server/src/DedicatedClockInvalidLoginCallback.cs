using scs.core;
using tecgraf.openbus;
using tecgraf.openbus.core.v2_00.services.offer_registry;

namespace Server {
  internal class DedicatedClockInvalidLoginCallback : InvalidLoginCallback {
    private readonly string _entity;
    private readonly byte[] _privKey;
    private readonly IComponent _ic;
    private readonly ServiceProperty[] _properties;

    public DedicatedClockInvalidLoginCallback(string entity, byte[] privKey,
                                              IComponent ic,
                                              ServiceProperty[] properties) {
      _entity = entity;
      _privKey = privKey;
      _ic = ic;
      _properties = properties;
    }

    public bool InvalidLogin(Connection conn) {
      //TODO O método de login deve tentar conectar e logar até conseguir, eternamente.
      return DedicatedClockServer.Login(_entity, _privKey) &&
             DedicatedClockServer.Register(_ic, _properties);
    }
  }
}