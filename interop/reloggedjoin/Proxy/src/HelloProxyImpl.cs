using System;
using tecgraf.openbus.assistant;
using tecgraf.openbus.core.v2_0.services.offer_registry;
using tecgraf.openbus.interop.simple;

namespace tecgraf.openbus.interop.relloggedjoin {
  public class HelloProxyImpl : MarshalByRefObject, Hello {
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
        Console.WriteLine("Refazendo o login da entidade: " + _entity);
        conn.Logout();
        conn.LoginByCertificate(_entity, _key);
        CallerChain chain = _context.CallerChain;
        string caller = chain.Caller.entity;
        Console.WriteLine("Chamada recebida de " + caller);

        ServiceProperty[] properties = new[]
        {new ServiceProperty("reloggedjoin.role", "server")};

        _context.JoinChain(chain);

        Console.WriteLine("Buscando servidor Hello relogged join");
        ServiceOfferDesc[] services =
          Utils.FilterWorkingOffers(
            _context.OfferRegistry.findServices(properties));
        foreach (ServiceOfferDesc desc in services) {
          string found = Utils.GetProperty(desc.properties,
                                           "openbus.offer.entity");
          Console.WriteLine("Serviço encontrado da entidade " + found);
          Hello hello = desc.service_ref.getFacetByName("Hello") as Hello;
          return hello.sayHello();
        }
      }
      catch (Exception e) {
        Console.WriteLine(e);
      }
      return "Erro ao utilizar o serviço Hello!";
    }
  }
}