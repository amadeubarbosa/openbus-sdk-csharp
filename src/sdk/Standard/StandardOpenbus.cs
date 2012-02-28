using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Security.Cryptography;
using Ch.Elca.Iiop;
using Ch.Elca.Iiop.Idl;
using tecgraf.openbus.core.v2_00.services.access_control;
using tecgraf.openbus.core.v2_00.services.offer_registry;
using tecgraf.openbus.sdk.Exceptions;
using log4net;
using omg.org.CORBA;
using scs.core;
using tecgraf.openbus.sdk.Security;
using tecgraf.openbus.sdk.Standard.Interceptors;

namespace tecgraf.openbus.sdk.Standard
{
  /// <summary>
  /// API de acesso a um barramento OpenBus.
  /// </summary>
  public class StandardOpenbus : Openbus
  {
    #region Fields

    private static readonly ILog Logger = LogManager.GetLogger(typeof(StandardOpenbus));

    private readonly String _host;
    private readonly long _port;
    private readonly AccessControl _acs;
    private readonly LoginRegistry _lr;
    private readonly OfferRegistry _or;
    private readonly IComponent _acsComponent;
    private readonly RSACryptoServiceProvider _busKey;
    private readonly Connection _connection;

    /// <summary>
    /// O Orb do IIOP.NET.
    /// <!-- Atenção: O orb é singleton, com isso só podemos registrar 
    /// interceptadores uma única vez. -->
    /// </summary>
    private readonly OrbServices _orb;

    /// <summary>
    /// O canal IIOP responsável pela troca de mensagens com o barramento.
    /// </summary>
    private readonly IiopChannel _channel;


    #endregion

    #region Constructors

    /// <summary>
    /// Construtor da classe StandardOpenbus
    /// </summary>
    public StandardOpenbus(String host, long port, bool hasServant) {
      if (string.IsNullOrEmpty(host))
        throw new ArgumentException("O campo 'host' não é válido");
      if (port < 0)
        throw new ArgumentException("O campo 'port' não pode ser negativo.");
      _host = host;
      _port = port;
      _connection = null;
      _acsComponent = RemotingServices.Connect(typeof(IComponent),
          "corbaloc::1.0@" + _host + ":" + _port + "/" + core.v2_00.BusObjectKey.ConstVal) as
          IComponent;

      if (_acsComponent == null) {
        Logger.Error("O serviço de controle de acesso não foi encontrado");
        throw new ACSUnavailableException();
      }

      String acsId = Repository.GetRepositoryID(typeof(AccessControl));
      String lrId = Repository.GetRepositoryID(typeof(LoginRegistry));
      String orId = Repository.GetRepositoryID(typeof(OfferRegistry));

      MarshalByRefObject acsObjRef;
      MarshalByRefObject lrObjRef;
      MarshalByRefObject orObjRef;
      try {
        acsObjRef = _acsComponent.getFacet(acsId);
        lrObjRef = _acsComponent.getFacet(lrId);
        orObjRef = _acsComponent.getFacet(orId);
      }
      catch (AbstractCORBASystemException e) {
        Logger.Error("O serviço de controle de acesso não foi encontrado", e);
        throw new ACSUnavailableException();
      }

      _acs = acsObjRef as AccessControl;
      _lr = lrObjRef as LoginRegistry;
      _or = orObjRef as OfferRegistry;
      if ((_acs == null) || (_lr == null) || (_or == null)) {
        Logger.Error("O serviço de controle de acesso não foi encontrado");
        return;
      }

      BusId = _acs.busid;
      _busKey = Crypto.ReadPrivateKey(_acs.buskey);

      // Adiciona interceptadores
      if (_orb == null) {
        _orb = OrbServices.GetSingleton();
        _orb.RegisterPortableInterceptorInitalizer(
            new StandardClientInitializer(this));
        _orb.RegisterPortableInterceptorInitalizer(
            new StandardServerInitializer(this, CredentialValidationPolicy.ALWAYS));
        _orb.CompleteInterceptorRegistration();
      }

      _channel = hasServant ? new IiopChannel(0) : new IiopChannel();
      ChannelServices.RegisterChannel(_channel, false);

      _connection = new StandardConnection(this);
    }

    #endregion

    #region Internal Members

    public RSACryptoServiceProvider BusKey {
      get { return _busKey; }
    }

    public AccessControl Acs {
      get { return _acs; }
    }

    public IComponent AcsComponent {
      get { return _acsComponent; }
    }

    public LoginRegistry LoginRegistry {
      get { return _lr; }
    }

    //TODO: apenas a OfferRegistry deveria ser publica
    public OfferRegistry OfferRegistry {
      get { return _or; }
    }

    public string BusId { get; private set; }

    public long Port {
      get { return _port; }
    }

    public string Host {
      get { return _host; }
    }
    #endregion

    #region Public Members

    /// <summary>
    /// Retorna a conexão com este barramento. Somente uma conexão é possível na API padrão.
    /// <returns>A conexão associada a esse barramento.</returns>
    /// </summary>
    public Connection Connect() {
      return _connection;
    }

    #endregion
  }
}
