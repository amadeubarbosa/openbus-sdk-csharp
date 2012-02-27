﻿using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using log4net;
using omg.org.CORBA;
using omg.org.IOP;
using tecgraf.openbus.core.v2_00.services.access_control;
using tecgraf.openbus.sdk.Exceptions;
using tecgraf.openbus.sdk.Security;

namespace tecgraf.openbus.sdk.Standard {
  internal class StandardConnection : Connection {
    #region Fields

    private static readonly ILog Logger =
      LogManager.GetLogger(typeof (StandardConnection));

    private readonly StandardOpenbus _bus;
    private readonly X509Certificate2 _certificate;
    private readonly RSACryptoServiceProvider _prvKey;
    private readonly RSACryptoServiceProvider _pubKey;
    private IExpiredLoginCallback _expLoginCallback;
    private LoginInfo? _login;

    #endregion

    #region Constructors

    public StandardConnection(StandardOpenbus bus) {
      _bus = bus;
      _certificate = Crypto.NewCertificate();
      _prvKey = Crypto.GetPrivateKey(_certificate);
      _pubKey = Crypto.GetPublicKey(_certificate);
      //TODO: Adicionar cache de logins
    }

    #endregion

    public LoginInfo? Login {
      get { return _login; }
    }

    public RSACryptoServiceProvider PublicKey {
      get { return _pubKey; }
    }

    public RSACryptoServiceProvider PrivateKey {
      get { return _prvKey; }
    }

    #region Connection Members

    void Connection.LoginByPassword(string entity, byte[] password) {
      if (IsLoggedIn()) {
        throw new ConnectionAlreadyLoggedIn();
      }

      Codec codec = GetCodec();

      byte[] encrypted;
      byte[] pubBlob = _pubKey.ExportCspBlob(false);
      try {
        //encode password and hash of public key
        LoginAuthenticationInfo info = new LoginAuthenticationInfo {
                                                                     data =
                                                                       password,
                                                                     hash =
                                                                       SHA256.
                                                                       Create().
                                                                       ComputeHash
                                                                       (pubBlob)
                                                                   };
        encrypted = Crypto.Encrypt(_bus.BusKey, codec.encode_value(info));
      }
      catch {
        const string msg = "Erro na codificação das informações de login.";
        Logger.Fatal(msg);
        throw new ACSLoginFailureException(msg);
      }

      int lease;
      _login = _bus.Acs.loginByPassword(entity, pubBlob, encrypted, out lease);
      //TODO: utilizar o lease
    }

    void Connection.LoginByCertificate(string entity, byte[] privKey) {
      if (IsLoggedIn()) {
        throw new ConnectionAlreadyLoggedIn();
      }
      byte[] challenge;
      LoginProcess login = _bus.Acs.startLoginByCertificate(entity,
                                                            out
                                                              challenge);
      byte[] answer = Crypto.Decrypt(_bus.BusKey, challenge);
      LoginByObject(login, answer);
    }

    public LoginProcess StartSingleSignOn(out byte[] secret) {
      byte[] challenge;
      LoginProcess login = _bus.Acs.startLoginBySingleSignOn(out challenge);
      secret = Crypto.Decrypt(_prvKey, challenge);
      return login;
    }

    public void LoginBySingleSignOn(LoginProcess login, byte[] secret) {
      LoginByObject(login, secret);
    }

    public bool IsLoggedIn() {
      return (_login == null);
    }

    public bool Logout() {
      if (!IsLoggedIn()) {
        return false;
      }

      try {
        _bus.Acs.logout();
      }
      catch (NO_PERMISSION e) {
        if ((e.Minor != InvalidLoginCode.ConstVal) ||
            (e.Status.Equals("COMPLETED_NO"))) {
          throw;
        }
        // já fui deslogado do barramento
        LocalLogout();
        return false;
      }
      LocalLogout();
      return true;
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

    private void LoginByObject(LoginProcess login, byte[] secret) {
      if (IsLoggedIn()) {
        login.cancel();
        throw new ConnectionAlreadyLoggedIn();
      }

      Codec codec = GetCodec();

      byte[] encrypted;
      byte[] pubBlob = _pubKey.ExportCspBlob(false);
      try {
        //encode answer and hash of public key
        LoginAuthenticationInfo info = new LoginAuthenticationInfo {
                                                                     data =
                                                                       secret,
                                                                     hash =
                                                                       SHA256.
                                                                       Create().
                                                                       ComputeHash
                                                                       (pubBlob)
                                                                   };
        encrypted = Crypto.Encrypt(_bus.BusKey, codec.encode_value(info));
      }
      catch {
        login.cancel();
        const string msg = "Erro na codificação das informações de login.";
        Logger.Fatal(msg);
        throw new ACSLoginFailureException(msg);
      }

      int lease;
      _login = login.login(pubBlob, encrypted, out lease);
      //TODO: utilizar o lease
    }

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

    protected void LocalLogout() {
      _login = null;
      //TODO: resetar caches qdo forem implementados
      //TODO: desagendar o renovador de credenciais
    }
  }
}