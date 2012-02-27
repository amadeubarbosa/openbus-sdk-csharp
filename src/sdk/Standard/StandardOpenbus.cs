using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Security.Cryptography;
using Ch.Elca.Iiop;
using Ch.Elca.Iiop.Idl;
using tecgraf.openbus.core.v2_00.services.access_control;
using tecgraf.openbus.sdk.Exceptions;
using tecgraf.openbus.sdk.Interceptors;
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
    private readonly IComponent _acsComponent;
    private readonly RSACryptoServiceProvider _busKey;
    private readonly Connection _connection;

    /// <summary>
    /// O Orb do IIOP.NET.
    /// <!-- Aten��o: O orb � singleton, com isso s� podemos registrar 
    /// interceptadores uma �nica vez. -->
    /// </summary>
    private readonly OrbServices _orb;

    /// <summary>
    /// O canal IIOP respons�vel pela troca de mensagens com o barramento.
    /// </summary>
    private readonly IiopChannel _channel;

    #endregion

    #region Constructors

    /// <summary>
    /// Construtor da classe StandardOpenbus
    /// </summary>
    public StandardOpenbus(String host, long port, bool hasServant) {
      if (string.IsNullOrEmpty(host))
        throw new ArgumentException("O campo 'host' n�o � v�lido");
      if (port < 0)
        throw new ArgumentException("O campo 'port' n�o pode ser negativo.");
      _host = host;
      _port = port;
      _connection = null;
      _acsComponent = RemotingServices.Connect(typeof(IComponent),
          "corbaloc::1.0@" + _host + ":" + _port + "/" + core.v2_00.BusObjectKey.ConstVal) as
          IComponent;

      if (_acsComponent == null) {
        Logger.Error("O servi�o de controle de acesso n�o foi encontrado");
        throw new ACSUnavailableException();
      }

      String acsId = Repository.GetRepositoryID(typeof(AccessControl));

      MarshalByRefObject acsObjRef;
      try {
        acsObjRef = _acsComponent.getFacet(acsId);
      }
      catch (AbstractCORBASystemException e) {
        Logger.Error("O servi�o de controle de acesso n�o foi encontrado", e);
        throw new ACSUnavailableException();
      }

      _acs = acsObjRef as AccessControl;
      if (_acs == null) {
        Logger.Error("O servi�o de controle de acesso n�o foi encontrado");
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
    /// Retorna a conex�o com este barramento. Somente uma conex�o � poss�vel na API padr�o.
    /// <returns>A conex�o associada a esse barramento.</returns>
    /// </summary>
    public Connection Connect() {
      return _connection;
    }

    #endregion
  }
}