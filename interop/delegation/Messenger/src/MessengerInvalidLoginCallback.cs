﻿using System;
using scs.core;
using tecgraf.openbus.core.v2_0.services.access_control;
using tecgraf.openbus.core.v2_0.services.offer_registry;
using tecgraf.openbus.exceptions;

namespace tecgraf.openbus.interop.delegation {
  internal class MessengerInvalidLoginCallback : InvalidLoginCallback {
    private readonly string _entity;
    private readonly byte[] _privKey;
    private readonly IComponent _ic;
    private readonly ServiceProperty[] _properties;

    internal MessengerInvalidLoginCallback(string entity, byte[] privKey,
                                           IComponent ic,
                                           ServiceProperty[] properties) {
      _entity = entity;
      _privKey = privKey;
      _ic = ic;
      _properties = properties;
    }

    public void InvalidLogin(Connection conn, LoginInfo login, string busId) {
      try {
        Console.WriteLine(
          "Callback de InvalidLogin foi chamada, tentando logar novamente no barramento.");
        conn.LoginByCertificate(_entity, _privKey);
        MessengerServer.Offer = conn.Offers.registerService(_ic, _properties);
      }
      catch (AlreadyLoggedInException) {
        // outra thread reconectou
      }
      catch (Exception e) {
        Console.WriteLine(e);
      }
    }
  }
}