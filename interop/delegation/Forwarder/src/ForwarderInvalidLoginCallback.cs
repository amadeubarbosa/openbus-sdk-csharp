﻿using System;
using tecgraf.openbus.core.v2_0.services.access_control;

namespace tecgraf.openbus.interop.delegation {
  class ForwarderInvalidLoginCallback : InvalidLoginCallback {
    private readonly string _entity;
    private readonly byte[] _privKey;
    private readonly ForwarderImpl _forwarder;

    public ForwarderInvalidLoginCallback(string entity, byte[] privKey, ForwarderImpl forwarder) {
      _entity = entity;
      _privKey = privKey;
      _forwarder = forwarder;
    }

    public void InvalidLogin(Connection conn, LoginInfo login, string busId) {
      try {
        Console.WriteLine("Callback de InvalidLogin foi chamada, tentando logar novamente no barramento.");
        conn.LoginByCertificate(_entity, _privKey);
        if (conn.Login == null) {
          _forwarder.Timer.Stop();
        }
      }
      catch (Exception e) {
        _forwarder.Timer.Stop();
        Console.WriteLine(e);
      }
    }
  }
}
