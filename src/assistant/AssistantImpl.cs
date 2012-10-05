using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using log4net;
using omg.org.CORBA;
using scs.core;
using tecgraf.openbus.assistant.callbacks;
using tecgraf.openbus.core.v2_0.services.access_control;
using tecgraf.openbus.core.v2_0.services.offer_registry;

namespace tecgraf.openbus.assistant {
  /// <inheritdoc/>
  public class AssistantImpl : Assistant {
    private static readonly ILog Logger = LogManager.GetLogger(typeof(AssistantImpl));

    private readonly string _host;
    private readonly ushort _port;
    private readonly AssistantProperties _properties;
    private readonly Connection _conn;
    private readonly IDictionary<IComponent, Offeror> _offers;
    private readonly ReaderWriterLockSlim _lock;

    /// <summary>
    /// Cria um assistente que efetua login no barramento utilizando o método 
    /// de autenticação definido pelo tipo de AssistantProperties fornecido.
    /// </summary>
    /// <param name="host">Endereço ou nome de rede onde os serviços núcleo do 
    /// barramento estão executando.</param>
    /// <param name="port">Porta onde os serviços núcleo do barramento estão 
    /// executando.</param>
    /// <param name="properties">Conjunto de parâmetros obrigatórios e 
    /// opcionais. Fornece também o método de autenticação a ser utilizado para
    /// efetuar login no barramento.</param>
    public AssistantImpl(string host, ushort port,
                         AssistantProperties properties) {
      OpenBusContext context = ORBInitializer.Context;
      _lock = new ReaderWriterLockSlim();
      Orb = context.ORB;
      _host = host;
      _port = port;
      _properties = properties;
      _offers = new Dictionary<IComponent, Offeror>();
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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public ServiceProperty[] UnregisterService(IComponent component) {
      _lock.EnterWriteLock();
      try {
        if (_offers.ContainsKey(component)) {
          _offers[component].Cancel();
          if (_offers.Remove(component)) {
            return _offers[component].Properties;
          }
        }
        return null;
      }
      finally {
        _lock.ExitWriteLock();
      }
    }

    /// <inheritdoc/>
    public ServiceOfferDesc[] FindServices(ServiceProperty[] properties,
                                           int retries) {
      return Find(properties, retries, false);
    }

    /// <inheritdoc/>
    public ServiceOfferDesc[] GetAllServices(int retries) {
      return Find(null, retries, true);
    }

    /// <inheritdoc/>
    public void Shutdown() {
      _lock.EnterWriteLock();
      try {
        foreach (KeyValuePair<IComponent, Offeror> pair in _offers) {
          pair.Value.Cancel();
        }
        _offers.Clear();
      }
      finally {
        _lock.ExitWriteLock();
      }
      _conn.Logout();
    }

    /// <inheritdoc/>
    public ORB Orb { get; private set; }

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
        Logger.Error("Erro ao tentar encontrar serviços.", caught);
        try {
          Properties.FindFailureCallback(this, caught);
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