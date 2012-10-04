using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using log4net;
using omg.org.CORBA;
using scs.core;
using tecgraf.openbus.assistant.callbacks;
using tecgraf.openbus.assistant.properties;
using tecgraf.openbus.core.v2_0.services.access_control;
using tecgraf.openbus.core.v2_0.services.offer_registry;

namespace tecgraf.openbus.assistant {
  /// <inheritdoc/>
  public class AssistantImpl : Assistant {
    private static readonly ILog Logger = LogManager.GetLogger(typeof (Offeror));

    private readonly string _host;
    private readonly ushort _port;
    private readonly AssistantProperties _properties;
    private readonly Connection _conn;
    private readonly IDictionary<IComponent, Offeror> _offers;
    private readonly ReaderWriterLockSlim _lock;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="host"></param>
    /// <param name="port"></param>
    /// <param name="properties"></param>
    public AssistantImpl(string host, ushort port,
                         AssistantProperties properties) {
      OpenBusContext context = ORBInitializer.Context;
      _lock = new ReaderWriterLockSlim();
      Orb = context.ORB;
      _host = host;
      _port = port;
      _properties = properties;
      _offers = new Dictionary<IComponent, Offeror>();
      if (properties.FailureCallback == null) {
        properties.FailureCallback = new AssistantOnFailureCallback();
      }
      // cria conexão e seta como padrão
      _conn = ORBInitializer.Context.CreateConnection(_host, _port,
                                                      properties.
                                                        ConnectionProperties);
      context.SetDefaultConnection(_conn);
      // adiciona callback de login inválido
      _conn.OnInvalidLogin = new AssistantOnInvalidLoginCallback(this);
      // lança a thread que faz o login inicial
      Thread t =
        new Thread(
          () => _conn.OnInvalidLogin.InvalidLogin(_conn, new LoginInfo()))
        {IsBackground = true};
      t.Start();
    }

    public void RegisterService(IComponent component,
                                ServiceProperty[] properties) {
      Offeror offeror = new Offeror(component, properties, this);
      _lock.EnterWriteLock();
      try {
        _offers.Add(component, offeror);
      }
      finally {
        _lock.ExitWriteLock();
      }
      // se não tiver login, próxima chamada à callback de login inválido vai relogar e registrar essa oferta
      if (_conn.Login.HasValue) {
        offeror.Activate();
      }
    }

    public ServiceProperty[] UnregisterService(IComponent component) {
      Offeror offeror;
      Offers.TryGetValue(component, out offeror);
      if (offeror != null) {
        offeror.Cancel();
        return offeror.Properties;
      }
      return null;
    }

    public ServiceOfferDesc[] FindServices(ServiceProperty[] properties,
                                           int retries) {
      return Find(properties, retries, false);
    }

    public ServiceOfferDesc[] GetAllServices(int retries) {
      return Find(null, retries, true);
    }

    public void SubscribeObserver(OfferRegistrationObserver observer,
                                  ServiceProperty[] properties) {
      throw new NotImplementedException();
    }

    public void Shutdown() {
      foreach (KeyValuePair<IComponent, Offeror> pair in Offers) {
        pair.Value.Cancel();
      }
      _conn.Logout();
    }

    public ORB Orb { get; private set; }

    //TODO jogar esses metodos estaticos pra um utilitario?
    /// <summary>
    /// 
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="facet"></param>
    /// <returns></returns>
    public static ServiceProperty[] NewSearchProperties(string entity,
                                                        string facet) {
      return new[] {
                     new ServiceProperty("openbus.offer.entity", entity),
                     new ServiceProperty("openbus.component.facet", facet)
                   };
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="properties"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static string GetProperty(IEnumerable<ServiceProperty> properties,
                                     string name) {
      return (from property in properties
              where property.name.Equals(name)
              select property.value).FirstOrDefault();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="offers"></param>
    /// <returns></returns>
    public static ServiceOffer[] GetActiveOffersOnly(
      IEnumerable<ServiceOffer> offers) {
      OrbServices orb = OrbServices.GetSingleton();
      return
        offers.Where(offer => orb.non_existent(offer.service_ref)).ToArray();
    }

    internal AssistantProperties Properties {
      get { return _properties; }
    }

    internal ConcurrentDictionary<IComponent, Offeror> Offers {
      get {
        // retorna uma cópia
        _lock.EnterReadLock();
        try {
          return new ConcurrentDictionary<IComponent, Offeror>(_offers);
        }
        finally {
          _lock.ExitReadLock();
        }
      }
    }

    private ServiceOfferDesc[] Find(ServiceProperty[] properties, int retries,
                                    bool all) {
      OpenBusContext context = ORBInitializer.Context;
      do {
        Exception caught;
        try {
          return all
                   ? context.OfferRegistry.getServices()
                   : context.OfferRegistry.findServices(properties);
        }
        catch (NO_PERMISSION e) {
          if (e.Minor == NoLoginCode.ConstVal) {
            caught = e;
          }
          else {
            throw;
          }
        }
        catch (Exception e) {
          caught = e;
        }
        Logger.Error("Erro ao tentar encontrar serviços.");
        try {
          Properties.FailureCallback.OnFindFailure(this, caught);
        }
        catch (Exception e) {
          Logger.Error(
            "Erro ao executar a callback de falha de busca fornecida pelo usuário.",
            e);
        }
        if (retries > 0) {
          Thread.Sleep(Properties.Interval * 1000);
          retries--;
        }
      } while (retries != 0);
      return new ServiceOfferDesc[0];
    }
  }
}