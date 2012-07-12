using System;
using tecgraf.openbus.core.v2_0.services.access_control;

namespace tecgraf.openbus.interop.simple {
  class HelloInvalidLoginCallback : InvalidLoginCallback {
    private readonly string _entity;
    private readonly byte[] _privKey;
    private readonly ConnectionManager _manager;

    public HelloInvalidLoginCallback(string entity, byte[] privKey, ConnectionManager manager) {
      _entity = entity;
      _privKey = privKey;
      _manager = manager;
    }

    public void InvalidLogin(Connection conn, LoginInfo login, string busId) {
      _manager.Requester = conn;
      try {
        Console.WriteLine("Callback de InvalidLogin foi chamada, tentando logar novamente no barramento.");
        conn.LoginByCertificate(_entity, _privKey);
      }
      catch (Exception e) {
        Console.WriteLine(e);
      }
    }
  }
}
