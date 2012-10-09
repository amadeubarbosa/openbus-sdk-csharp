using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using log4net;
using omg.org.CORBA;
using scs.core;
using tecgraf.openbus.core.v2_0.services.access_control;
using tecgraf.openbus.core.v2_0.services.offer_registry;
using tecgraf.openbus.exceptions;

namespace tecgraf.openbus.assistant {
  /// <inheritdoc/>
  public class AssistantImpl : Assistant {
    private static readonly ILog Logger =
      LogManager.GetLogger(typeof (AssistantImpl));

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
    /// <exception cref="ArgumentException">Caso o parâmetro properties não 
    /// seja uma instância de PasswordProperties, PrivateKeyProperties ou 
    /// SharedAuthProperties.</exception>
    public AssistantImpl(string host, ushort port,
                         AssistantProperties properties) {
      if ((properties as PasswordProperties == null) &&
          (properties as PrivateKeyProperties == null) &&
          (properties as SharedAuthProperties == null)) {
        throw new ArgumentException(
          "O parâmetro properties deve ser uma instância de PasswordProperties, PrivateKeyProperties ou SharedAuthProperties.");
      }
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
      _conn.OnInvalidLogin = InvalidLogin;
      // lança a thread que faz o login inicial
      Thread t =
        new Thread(
          () => _conn.OnInvalidLogin(_conn, new LoginInfo()))
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
    public LoginProcess StartSharedAuth(out byte[] secret, int retries) {
      do {
        Exception caught;
        try {
          return _conn.StartSharedAuth(out secret);
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
        Logger.Error("Erro ao tentar iniciar uma autenticação compartilhada.",
                     caught);
        try {
          Properties.StartSharedAuthFailureCallback(this, caught);
        }
        catch (Exception e) {
          Logger.Error(
            "Erro ao executar a callback de falha de inicialização de autenticação compartilhada fornecida pelo usuário.",
            e);
        }
        if (retries > 0) {
          Thread.Sleep(Properties.Interval * 1000);
          retries--;
        }
      } while (retries != 0);
      secret = null;
      return null;
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

    private void InvalidLogin(Connection conn, LoginInfo login) {
      bool succeeded = false;
      while (!succeeded) {
        // remove todas as ofertas locais
        foreach (KeyValuePair<IComponent, Offeror> pair in Offers) {
          pair.Value.Reset();
        }
        Exception caught = null;
        try {
          switch (Properties.Type) {
            case LoginType.Password:
              PasswordProperties passwordProps = (PasswordProperties) Properties;
              conn.LoginByPassword(passwordProps.Entity, passwordProps.Password);
              break;
            case LoginType.PrivateKey:
              PrivateKeyProperties privKeyProps =
                (PrivateKeyProperties) Properties;
              conn.LoginByCertificate(privKeyProps.Entity,
                                      privKeyProps.PrivateKey);
              break;
            case LoginType.SharedAuth:
              SharedAuthProperties saProps = (SharedAuthProperties) Properties;
              byte[] secret;
              LoginProcess lp = saProps.Callback.Invoke(out secret);
              conn.LoginBySharedAuth(lp, secret);
              break;
          }
          succeeded = true;
        }
        catch (AlreadyLoggedInException e) {
          caught = e;
          succeeded = true;
        }
        catch (NO_PERMISSION e) {
          if (e.Minor == NoLoginCode.ConstVal) {
            caught = e;
          }
          else {
            // provavelmente é um buug, então deixa estourar
            throw;
          }
        }
        catch (Exception e) {
          // passa qualquer erro para a aplicação, menos erros que pareçam bugs do SDK
          caught = e;
        }
        if (succeeded) {
          // Se houve exceção recebida e foi AlreadyLoggedIn, já existe uma thread tentando registrar as ofertas
          if ((caught as AlreadyLoggedInException) == null) {
            // Inicia o processo de re-registro das ofertas
            foreach (KeyValuePair<IComponent, Offeror> pair in Offers) {
              pair.Value.Activate();
            }
          }
        }
        else {
          try {
            Properties.LoginFailureCallback(this, caught);
          }
          catch (Exception e) {
            Logger.Error(
              "Erro ao executar a callback de falha de login fornecida pelo usuário.",
              e);
          }
          Thread.Sleep(Properties.Interval * 1000);
        }
      }
    }
  }
}