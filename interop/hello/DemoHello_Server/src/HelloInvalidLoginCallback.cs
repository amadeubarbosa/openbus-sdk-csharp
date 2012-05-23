using System;

namespace tecgraf.openbus.interop.hello {
  class HelloInvalidLoginCallback : InvalidLoginCallback {
    private readonly string _entity;
    private readonly byte[] _privKey;
    private readonly ConnectionManager _manager;

    public HelloInvalidLoginCallback(string entity, byte[] privKey, ConnectionManager manager) {
      _entity = entity;
      _privKey = privKey;
      _manager = manager;
    }

    public bool InvalidLogin(Connection conn) {
      _manager.ThreadRequester = conn;
      try {
        Console.WriteLine("Callback de InvalidLogin foi chamada, tentando logar novamente no barramento.");
        conn.LoginByCertificate(_entity, _privKey);
        return conn.Login != null;
      }
      catch (Exception e) {
        Console.WriteLine(e.StackTrace);
      }
      return false;
    }
  }
}
