using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using log4net;
using omg.org.CORBA;
using omg.org.IOP;
using tecgraf.openbus.core.v2_00.services.access_control;
using tecgraf.openbus.sdk.Exceptions;
using tecgraf.openbus.sdk.Security;

namespace tecgraf.openbus.sdk.Multiplexed {
  internal class MultiplexedConnection : Connection {
    #region Fields

    private static readonly ILog Logger =
      LogManager.GetLogger(typeof (MultiplexedConnection));

    private readonly Openbus _bus;
    private readonly X509Certificate2 _certificate;
    private readonly RSACryptoServiceProvider _prvKey;
    private readonly RSACryptoServiceProvider _pubKey;
    private IExpiredLoginCallback _expLoginCallback;
    private LoginInfo? _login;

    #endregion

    #region Constructors

    public MultiplexedConnection(Openbus bus) {
      _bus = bus;
      _certificate = Crypto.NewCertificate();
      _prvKey = Crypto.GetPrivateKey(_certificate);
      _pubKey = Crypto.GetPublicKey(_certificate);
      //TODO: Adicionar cache de logins
    }

    #endregion

    #region Connection Members

    void Connection.LoginByPassword(string entity, byte[] password) {
    }

    void Connection.LoginByCertificate(string entity, byte[] privKey) {
    }

    public bool IsLoggedIn() {
      return (_login == null);
    }

    public bool Logout() {
      if (IsLoggedIn()) {
        try {
          _bus.Acs.logout();
        }
        catch(Exception e) {
          //TODO: implementar o resto
        }
      }
      throw new NotImplementedException();
    }

    void Connection.SetExpiredLoginCallback(IExpiredLoginCallback callback) {
      _expLoginCallback = callback;
    }

    IExpiredLoginCallback Connection.GetExpiredLoginCallback() {
      return _expLoginCallback;
    }

    CallChain Connection.GetCallerChain() {
      throw new NotImplementedException();
    }

    void Connection.JoinChain(CallChain chain) {
      throw new NotImplementedException();
    }

    void Connection.ExitChain() {
      throw new NotImplementedException();
    }

    CallChain Connection.GetJoinedChain() {
      throw new NotImplementedException();
    }

    void Connection.Close() {
      Logout();
      //TODO: remover conexao de cache se for o caso
    }

    #endregion

    private static Codec GetCodec() {
      OrbServices orb = OrbServices.GetSingleton();
      CodecFactory factory =
        orb.resolve_initial_references("CodecFactory") as CodecFactory;
      if (factory == null) {
        throw new OpenbusException("CodecFactory is null, cannot encode data.");
      }
      Encoding encode = new Encoding(ENCODING_CDR_ENCAPS.ConstVal, 1, 2);
      Codec codec = factory.create_codec(encode);
      return codec;
    }
  }
}