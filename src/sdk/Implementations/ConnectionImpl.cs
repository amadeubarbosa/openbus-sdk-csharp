using System;
using System.Runtime.Remoting;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Ch.Elca.Iiop.Idl;
using log4net;
using omg.org.CORBA;
using omg.org.IOP;
using scs.core;
using tecgraf.openbus.core.v2_00.services.access_control;
using tecgraf.openbus.sdk.Exceptions;
using tecgraf.openbus.sdk.Security;

namespace tecgraf.openbus.sdk.Implementations {
  internal class ConnectionImpl : IConnection {
    #region Fields

    private static readonly ILog Logger = LogManager.GetLogger(typeof(ConnectionImpl));

    private String _host;
    private Login _login;
    private long _port;
    private IComponent _acsComponent;
    private AccessControl _acs;
    private String _busId;
    private RSACryptoServiceProvider _busKey;
    private RSACryptoServiceProvider _prvKey;
    //TODO: remover a inicializacao estatica abaixo, talvez fique na classe openbus
    private String OPENBUS_KEY = "openbus_v2_00";

    #endregion

    #region Constructors

    public ConnectionImpl(String host, long port) {
      if (string.IsNullOrEmpty(host))
        throw new ArgumentException("O campo 'host' não é válido");
      if (port < 0)
        throw new ArgumentException("O campo 'port' não pode ser negativo.");
      _host = host;
      _port = port;

      _acsComponent = RemotingServices.Connect(typeof(IComponent),
          "corbaloc::1.0@" + host + ":" + port + "/" + OPENBUS_KEY) as
          IComponent;

      if (_acsComponent == null) {
        Logger.Error("O serviço de controle de acesso não foi encontrado");
        throw new ACSUnavailableException();
      }

      String acsId = Repository.GetRepositoryID(typeof(AccessControl));

      MarshalByRefObject acsObjRef;
      try {
        acsObjRef = _acsComponent.getFacet(acsId);
      }
      catch (AbstractCORBASystemException e) {
        Logger.Error("O serviço de controle de acesso não foi encontrado", e);
        throw new ACSUnavailableException();
      }

      _acs = acsObjRef as AccessControl;
      if (_acs == null) {
        Logger.Error("O serviço de controle de acesso não foi encontrado");
        return;
      }

      _busId = _acs.busid;
      _busKey = Crypto.ReadPrivateKey(_acs.buskey);
      _prvKey = Crypto.NewKey();

      //TODO: Adicionar cache de logins
    }

    #endregion

    #region IConnection Members

    void IConnection.LoginByPassword(string entity, byte[] password) {
      if (IsLoggedIn()) {
        throw new ConnectionAlreadyLoggedIn();
      }
      //TODO: adicionar dados no request. Como obter o codec? Verificar se o codigo abaixo funciona
      OrbServices orb = OrbServices.GetSingleton();
      CodecFactory factory = orb.resolve_initial_references("CodecFactory") as CodecFactory;
      if (factory == null) {
        throw new OpenbusException("CodecFactory is null, cannot encode data.");
      }
      Encoding encode = new Encoding(ENCODING_CDR_ENCAPS.ConstVal, 1, 2);
      Codec codec = factory.create_codec(encode);

      byte[] value = null;
      try {
        //encode password and hash of public key
        LoginAuthenticationInfo info = new LoginAuthenticationInfo();
        info.data = password;
        info.hash = SHA256.Create().ComputeHash(pubKey);
        value = codec.encode_value(info);
      }
      catch {
        Logger.Fatal("Erro na codificação das informações de login.");
      }
      ServiceContext serviceContext = new ServiceContext(CONTEXT_ID, value);
      ri.add_request_service_context(serviceContext, false);
      */
      int lease;
      String id = _acs.loginByPassword(entity, pubKey, encrypted, out lease);
      _login = new Login(entity, id, pubKey);
    }

    void IConnection.LoginByCertificate(string entity, byte[] privKey) {
      throw new NotImplementedException();
    }

    void IConnection.ShareLogin(byte[] encodedlogin) {
      throw new NotImplementedException();
    }

    bool IConnection.Logout() {
      throw new NotImplementedException();
    }

    void IConnection.SetExpiredLoginCallback(IExpiredLoginCallback callback) {
      throw new NotImplementedException();
    }

    IExpiredLoginCallback IConnection.GetExpiredLoginCallback() {
      throw new NotImplementedException();
    }

    CallChain IConnection.GetCallerChain() {
      throw new NotImplementedException();
    }

    void IConnection.JoinChain(CallChain chain) {
      throw new NotImplementedException();
    }

    void IConnection.ExitChain() {
      throw new NotImplementedException();
    }

    CallChain IConnection.GetJoinedChain() {
      throw new NotImplementedException();
    }

    void IConnection.Close() {
      throw new NotImplementedException();
    }

    #endregion

    private bool IsLoggedIn() {
      return (_login == null);
    }

    #region Nested type: Login

    private class Login {
      private readonly String _entity;
      private readonly String _identifier;
      private readonly RSACryptoServiceProvider _publicKey;

      public Login(String entity, String id, RSACryptoServiceProvider publicKey) {
        _entity = entity;
        _identifier = id;
        _publicKey = publicKey;
      }

      public RSACryptoServiceProvider PublicKey {
        get { return _publicKey; }
      }

      public string Identifier {
        get { return _identifier; }
      }

      public string Entity {
        get { return _entity; }
      }
    }

    #endregion
  }
}