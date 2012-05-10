using System;

namespace tecgraf.openbus.demo.delegation {
  class ForwarderInvalidLoginCallback : InvalidLoginCallback {
    private readonly string _entity;
    private readonly byte[] _privKey;
    private readonly ForwarderImpl _forwarder;

    public ForwarderInvalidLoginCallback(string entity, byte[] privKey, ForwarderImpl forwarder) {
      _entity = entity;
      _privKey = privKey;
      _forwarder = forwarder;
    }

    public bool InvalidLogin(Connection conn) {
      try {
        Console.WriteLine("Callback de InvalidLogin foi chamada, tentando logar novamente no barramento.");
        conn.LoginByCertificate(_entity, _privKey);
        if (conn.Login == null) {
          _forwarder.Timer.Stop();
          return false;
        }
        return true;
      }
      catch (Exception e) {
        _forwarder.Timer.Stop();
        Console.WriteLine(e.StackTrace);
      }
      return false;
    }
  }
}
