using System;
using tecgraf.openbus.core.v2_0.services.access_control;

namespace tecgraf.openbus.interop.multiplexing {
  internal class HelloInvalidLoginCallback : InvalidLoginCallback {
    private readonly string _entity;
    private readonly byte[] _key;

    internal HelloInvalidLoginCallback(string entity, byte[] privateKey) {
      _entity = entity;
      _key = privateKey;
    }

    public void InvalidLogin(Connection conn, LoginInfo login, string busId) {
      try {
        Console.WriteLine("Callback de InvalidLogin da conexão " + _entity +
                          " foi chamada, tentando logar novamente no barramento.");
        conn.LoginByCertificate(_entity, _key);
      }
      catch (Exception e) {
        Console.WriteLine(e);
      }
    }
  }
}