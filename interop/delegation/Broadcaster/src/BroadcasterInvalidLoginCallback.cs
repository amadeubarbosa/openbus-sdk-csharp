using System;
using scs.core;
using tecgraf.openbus.core.v2_0.services.access_control;
using tecgraf.openbus.core.v2_0.services.offer_registry;
using tecgraf.openbus.exceptions;

namespace tecgraf.openbus.interop.delegation {
  internal class BroadcasterInvalidLoginCallback : InvalidLoginCallback {
    private readonly string _entity;
    private readonly PrivateKey _privKey;
    private readonly IComponent _ic;
    private readonly ServiceProperty[] _properties;

    internal BroadcasterInvalidLoginCallback(string entity, PrivateKey privKey,
                                             IComponent ic,
                                             ServiceProperty[] properties) {
      _entity = entity;
      _privKey = privKey;
      _ic = ic;
      _properties = properties;
    }

    public void InvalidLogin(Connection conn, LoginInfo login) {
      try {
        Console.WriteLine(
          "Callback de InvalidLogin foi chamada, tentando logar novamente no barramento.");
        conn.LoginByCertificate(_entity, _privKey);
        BroadcasterServer.Offer =
          ORBInitializer.Context.OfferRegistry.registerService(_ic, _properties);
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