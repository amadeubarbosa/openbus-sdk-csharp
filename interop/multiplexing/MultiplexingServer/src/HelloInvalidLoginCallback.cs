﻿using System;
using tecgraf.openbus.core.v2_0.services.access_control;

namespace tecgraf.openbus.interop.multiplexing {
  class HelloInvalidLoginCallback : InvalidLoginCallback {
    private readonly string _entity;
    private readonly byte[] _key;

    public HelloInvalidLoginCallback(string entity, byte[] privateKey) {
      _entity = entity;
      _key = privateKey;
    }

    public bool InvalidLogin(Connection conn, LoginInfo login, string busId) {
      try {
        Console.WriteLine("Callback de InvalidLogin da conexão " + _entity + " foi chamada, tentando logar novamente no barramento.");
        conn.LoginByCertificate(_entity, _key);
        return conn.Login != null;
      }
      catch (Exception e) {
        Console.WriteLine(e);
      }
      return false;
    }
  }
}
