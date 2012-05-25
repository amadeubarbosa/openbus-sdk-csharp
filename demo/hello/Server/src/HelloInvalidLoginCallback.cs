using System;
using scs.core;
using tecgraf.openbus.core.v2_00.services;
using tecgraf.openbus.core.v2_00.services.access_control;
using tecgraf.openbus.core.v2_00.services.offer_registry;
using tecgraf.openbus.exceptions;

namespace tecgraf.openbus.demo.hello {
  class HelloInvalidLoginCallback : InvalidLoginCallback {
    private readonly string _entity;
    private readonly byte[] _privKey;
    private readonly IComponent _ic;
    private readonly ServiceProperty[] _properties;
    private readonly ConnectionManager _manager;

    public HelloInvalidLoginCallback(string entity, byte[] privKey, IComponent ic, ServiceProperty[] properties) {
      _entity = entity;
      _privKey = privKey;
      _ic = ic;
      _properties = properties;
      _manager = ORBInitializer.Manager;
    }

    public bool InvalidLogin(Connection conn) {
      return HelloServer.Login(_entity, _privKey) && HelloServer.Register(_ic, _properties);
    }
  }
}
