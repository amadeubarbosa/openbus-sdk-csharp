using System;
using scs.core;
using tecgraf.openbus.core.v2_0.services.access_control;
using tecgraf.openbus.core.v2_0.services.offer_registry;
using tecgraf.openbus.exceptions;

namespace tecgraf.openbus.interop.multiplexing {
  internal class HelloInvalidLoginCallback : InvalidLoginCallback {
    private readonly string _entity;
    private readonly byte[] _key;
    private readonly IComponent _ic;
    private readonly ServiceProperty[] _properties;

    internal HelloInvalidLoginCallback(string entity, byte[] privateKey,
                                       IComponent ic,
                                       ServiceProperty[] properties) {
      _entity = entity;
      _key = privateKey;
      _ic = ic;
      _properties = properties;
    }

    public void InvalidLogin(Connection conn, LoginInfo login) {
      try {
        Console.WriteLine("Callback de InvalidLogin da conexão " + _entity +
                          " foi chamada, tentando logar novamente no barramento.");
        conn.LoginByCertificate(_entity, _key);
        conn.Offers.registerService(_ic, _properties);
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