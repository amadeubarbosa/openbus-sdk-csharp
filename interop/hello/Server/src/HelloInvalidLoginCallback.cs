using System;
using scs.core;
using tecgraf.openbus.core.v2_0.services.access_control;
using tecgraf.openbus.core.v2_0.services.offer_registry;
using tecgraf.openbus.exceptions;

namespace tecgraf.openbus.interop.simple {
  internal class HelloInvalidLoginCallback : InvalidLoginCallback {
    private readonly string _entity;
    private readonly PrivateKey _privKey;
    private readonly IComponent _ic;
    private readonly ServiceProperty[] _properties;

    internal HelloInvalidLoginCallback(string entity, PrivateKey privKey,
                                       IComponent ic,
                                       ServiceProperty[] properties) {
      _entity = entity;
      _privKey = privKey;
      _ic = ic;
      _properties = properties;
    }

    public void InvalidLogin(Connection conn, LoginInfo login) {
      OpenBusContext context = ORBInitializer.Context;
      context.SetCurrentConnection(conn);
      try {
        Console.WriteLine(
          "Callback de InvalidLogin foi chamada, tentando logar novamente no barramento.");
        conn.LoginByCertificate(_entity, _privKey);
        HelloServer.Offer = context.OfferRegistry.registerService(_ic, _properties);
      }
      catch(AlreadyLoggedInException) {
          // outra thread reconectou
      }
      catch (Exception e) {
        Console.WriteLine(e);
      }
    }
  }
}