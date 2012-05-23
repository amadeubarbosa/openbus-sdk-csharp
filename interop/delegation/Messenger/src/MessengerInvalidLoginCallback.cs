using System;

namespace tecgraf.openbus.interop.delegation {
  class MessengerInvalidLoginCallback : InvalidLoginCallback {
    private readonly string _entity;
    private readonly byte[] _privKey;

    public MessengerInvalidLoginCallback(string entity, byte[] privKey) {
      _entity = entity;
      _privKey = privKey;
    }

    public bool InvalidLogin(Connection conn) {
      try {
        Console.WriteLine("Callback de InvalidLogin foi chamada, tentando logar novamente no barramento.");
        conn.LoginByCertificate(_entity, _privKey);
        return (conn.Login != null);
      }
      catch (Exception e) {
        Console.WriteLine(e.StackTrace);
      }
      return false;
    }
  }
}
