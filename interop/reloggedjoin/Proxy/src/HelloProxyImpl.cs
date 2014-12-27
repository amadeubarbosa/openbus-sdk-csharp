using System;
using System.Collections.Generic;
using System.Linq;
using omg.org.CORBA;
using tecgraf.openbus.core.v2_1.services.offer_registry;
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

        ServiceProperty[] properties = {new ServiceProperty("reloggedjoin.role", "server")};

        _context.JoinChain(chain);

        Console.WriteLine("Buscando servidor Hello relogged join");
        ServiceOfferDesc[] services =
          FilterWorkingOffers(
            _context.OfferRegistry.findServices(properties));
        foreach (ServiceOfferDesc desc in services) {
          string found = GetProperty(desc.properties,
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

    static string GetProperty(IEnumerable<ServiceProperty> properties,
                                     string name) {
      return (from property in properties
              where property.name.Equals(name)
              select property.value).FirstOrDefault();
    }

    static ServiceOfferDesc[] FilterWorkingOffers(
      IEnumerable<ServiceOfferDesc> offers) {
      OrbServices orb = OrbServices.GetSingleton();
      IList<ServiceOfferDesc> working = new List<ServiceOfferDesc>();
      foreach (ServiceOfferDesc offerDesc in offers) {
        try {
          if (!orb.non_existent(offerDesc.service_ref)) {
            working.Add(offerDesc);
          }
        }
        // ReSharper disable EmptyGeneralCatchClause
        catch (Exception) {
          // ReSharper restore EmptyGeneralCatchClause
          // não adiciona essa oferta
        }
      }
      return working.ToArray();
    }
  }
}