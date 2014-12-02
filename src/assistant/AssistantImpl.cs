using System;
using System.Threading;
using log4net;
using omg.org.CORBA;
using scs.core;
using tecgraf.openbus.core.v2_1.services.access_control;
using tecgraf.openbus.core.v2_1.services.offer_registry;
using tecgraf.openbus.exceptions;

namespace tecgraf.openbus.assistant {
  /// <inheritdoc/>
  public class AssistantImpl : Assistant {
    #region Private Fields

    private static readonly ILog Logger =
      LogManager.GetLogger(typeof (AssistantImpl));

    private readonly string _host;
    private readonly ushort _port;
    private readonly AssistantPropertiesImpl _properties;
    private readonly Connection _conn;
    private readonly Offeror _offeror;
    private volatile bool _active;
    private readonly Thread _connThread;

    #endregion

    #region Constructor

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
      ORB = ORBInitializer.InitORB();
      OpenBusContext context = ORBInitializer.Context;
      _host = host;
      _port = port;
      _properties = properties as AssistantPropertiesImpl;
      _active = true;
      _offeror = new Offeror(this);
      // cria conexão e seta como padrão
      _conn = ORBInitializer.Context.ConnectByAddress(_host, _port, properties.ConnectionProperties);
      context.SetDefaultConnection(_conn);
      // adiciona callback de login inválido
      _conn.OnInvalidLogin = InvalidLogin;
      // lança a thread que faz o login inicial
      _connThread =
        new Thread(
          () => _conn.OnInvalidLogin(_conn, new LoginInfo())) { Name = "AssistantLogin", IsBackground = true };
      _connThread.Start();
    }

    #endregion

    #region Assistant Interface

    /// <inheritdoc/>
    public void RegisterService(IComponent component,
                                ServiceProperty[] properties) {
      _offeror.AddOffer(component, properties);
      // se não tiver login, próxima chamada à callback de login inválido vai relogar e registrar essa oferta
      if (_conn.Login.HasValue) {
        _offeror.Activate();
      }
    }

    /// <inheritdoc/>
    public void UnregisterService(IComponent component) {
      _offeror.RemoveOffers(component);
    }

    /// <inheritdoc/>
    public void UnregisterAll() {
      _offeror.RemoveAllOffers();
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
    public SharedAuthSecret StartSharedAuth(int retries) {
      do {
        Exception caught;
        try {
          return _conn.StartSharedAuth();
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
          if (Properties.StartSharedAuthFailureCallback != null) {
            Properties.StartSharedAuthFailureCallback(this, caught);
          }
        }
        catch (Exception e) {
          Logger.Error(
            "Erro ao executar a callback de falha de inicialização de autenticação compartilhada fornecida pelo usuário.",
            e);
        }
        if (retries == 0) {
          Logger.Warn(
            "Número de tentativas esgotado ao tentar iniciar uma autenticação compartilhada. A última exceção recebida será lançada.");
          throw caught;
        }
        if (retries > 0) {
          Logger.Debug("Erro ao tentar iniciar uma autenticação compartilhada. Uma nova tentativa será realizada.");
          retries--;
        }
        Thread.Sleep(Properties.IntervalMillis);
      } while (_active);

      Logger.Warn("O Assistente foi finalizado. Finalizando login por autenticação compartilhada.");
      return null;
    }

    /// <inheritdoc/>
    public void Shutdown() {
      _active = false;
      _connThread.Interrupt();
      _offeror.Finish();
      _conn.Logout();
    }

    /// <inheritdoc/>
    public OrbServices ORB { get; private set; }

    #endregion

    #region Internal Methods

    internal AssistantPropertiesImpl Properties {
      get { return _properties; }
    }

    private ServiceOfferDesc[] Find(ServiceProperty[] properties, int retries,
                                    bool all) {
      OpenBusContext context = ORBInitializer.Context;
      do {
        Exception caught;
        try {
          return all
                   ? context.OfferRegistry.getAllServices()
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
          if (Properties.FindFailureCallback != null) {
            Properties.FindFailureCallback(this, caught);
          }
        }
        catch (Exception e) {
          Logger.Error(
            "Erro ao executar a callback de falha de busca fornecida pelo usuário.",
            e);
        }
        if (retries == 0) {
          Logger.Warn(
            "Número de tentativas esgotado ao tentar encontrar serviços. Será retornada uma lista vazia.");
          return new ServiceOfferDesc[0];
        }
        if (retries > 0) {
          Logger.Debug("Erro ao tentar encontrar serviços. Uma nova tentativa será realizada.");
          retries--;
        }
        Thread.Sleep(Properties.IntervalMillis);
      } while (_active);

      Logger.Warn("O Assistente foi finalizado. Finalizando busca.");
      return new ServiceOfferDesc[0];
    }

    #endregion

    #region OnInvalidLogin

    private void InvalidLogin(Connection conn, LoginInfo login) {
      try {
        bool succeeded = false;
        while (!succeeded)
        {
          // remove todas as ofertas locais
          _offeror.ResetAllOffers();
          Exception caught = null;
          try
          {
            switch (Properties.Type)
            {
              case LoginType.Password:
                PasswordProperties passwordProps = (PasswordProperties)Properties;
                conn.LoginByPassword(passwordProps.Entity, passwordProps.Password, passwordProps.Domain);
                break;
              case LoginType.PrivateKey:
                PrivateKeyProperties privKeyProps =
                  (PrivateKeyProperties)Properties;
                conn.LoginByCertificate(privKeyProps.Entity,
                                        privKeyProps.PrivateKey);
                break;
              case LoginType.SharedAuth:
                SharedAuthProperties saProps = (SharedAuthProperties)Properties;
                SharedAuthSecret secret = saProps.Callback.Invoke();
                conn.LoginBySharedAuth(secret);
                break;
            }
            succeeded = true;
          }
          catch (AlreadyLoggedInException e)
          {
            caught = e;
            succeeded = true;
          }
          catch (NO_PERMISSION e)
          {
            if (e.Minor == NoLoginCode.ConstVal)
            {
              caught = e;
            }
            else
            {
              // provavelmente é um buug, então deixa estourar
              throw;
            }
          }
          catch (Exception e)
          {
            // passa qualquer erro para a aplicação, menos erros que pareçam bugs do SDK
            caught = e;
          }
          if (succeeded)
          {
            _offeror.Activate();
          }
          else
          {
            try
            {
              if (Properties.LoginFailureCallback != null)
              {
                Properties.LoginFailureCallback(this, caught);
              }
            }
            catch (Exception e)
            {
              Logger.Error(
                "Erro ao executar a callback de falha de login fornecida pelo usuário.",
                e);
            }
            Thread.Sleep(Properties.IntervalMillis);
          }
        }
      }
      catch (ThreadInterruptedException) {
        Logger.Warn("O assistente foi finalizado.");
      }
    }

    #endregion
  }
}