using System;
using tecgraf.openbus.core.v2_0.services.access_control;

namespace tecgraf.openbus.interop.delegation {
  class BroadcasterInvalidLoginCallback : InvalidLoginCallback {
    private readonly string _entity;
    private readonly byte[] _privKey;

    public BroadcasterInvalidLoginCallback(string entity, byte[] privKey) {
      _entity = entity;
      _privKey = privKey;
    }

    public bool InvalidLogin(Connection conn, LoginInfo login, string busId) {
      try {
        Console.WriteLine("Callback de InvalidLogin foi chamada, tentando logar novamente no barramento.");
        conn.LoginByCertificate(_entity, _privKey);
        return (conn.Login != null);
      }
      catch (Exception e) {
        Console.WriteLine(e);
      }
      return false;
    }
  }
}
