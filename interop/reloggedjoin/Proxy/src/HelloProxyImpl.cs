using System;
using System.Collections.Generic;
using log4net;
using tecgraf.openbus.core.v2_1.services.offer_registry;
using tecgraf.openbus.interop.simple;
using tecgraf.openbus.interop.utils;

namespace tecgraf.openbus.interop.relloggedjoin {
  public class HelloProxyImpl : MarshalByRefObject, Hello {
    private static readonly ILog Logger =
      LogManager.GetLogger(typeof(HelloProxyImpl));

    private readonly OpenBusContext _context;
    private readonly string _entity;
    private readonly PrivateKey _key;

    #region Constructors

    internal HelloProxyImpl(string entity, PrivateKey key) {
      _context = ORBInitializer.Context;
      _entity = entity;
      _key = key;
    }

    #endregion

    public string sayHello() {
      try {
        Connection conn = _context.GetCurrentConnection();
        Logger.Info("Refazendo o login da entidade: " + _entity);
        conn.Logout();
        conn.LoginByCertificate(_entity, _key);
        CallerChain chain = _context.CallerChain;
        string caller = chain.Caller.entity;
        Logger.Info("Chamada recebida de " + caller);

        ServiceProperty[] properties = {new ServiceProperty("reloggedjoin.role", "server")};

        _context.JoinChain(chain);

        Logger.Info("Buscando servidor Hello relogged join");
        List<ServiceOfferDesc> offers =
          Utils.FindOffer(ORBInitializer.Context.OfferRegistry, properties, 1, 10, 1);
        foreach (ServiceOfferDesc desc in offers) {
          string found = Utils.GetProperty(desc.properties,
                                           "openbus.offer.entity");
          Logger.Info("Serviço encontrado da entidade " + found);
          Hello hello = desc.service_ref.getFacetByName("Hello") as Hello;
          return hello.sayHello();
        }
      }
      catch (Exception e) {
        Logger.Info(e);
      }
      return "Erro ao utilizar o serviço Hello!";
    }
  }
}