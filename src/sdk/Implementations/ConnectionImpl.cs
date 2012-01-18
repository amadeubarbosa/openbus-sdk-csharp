﻿using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using log4net;
using omg.org.CORBA;
using omg.org.IOP;
using tecgraf.openbus.core.v2_00.services.access_control;
using tecgraf.openbus.sdk.Exceptions;
using tecgraf.openbus.sdk.Security;

namespace tecgraf.openbus.sdk.Implementations {
  internal class ConnectionImpl : IConnection {
    #region Fields

    private static readonly ILog Logger =
      LogManager.GetLogger(typeof (ConnectionImpl));

    private readonly Openbus _bus;
    private readonly X509Certificate2 _certificate;
    private readonly RSACryptoServiceProvider _prvKey;
    private readonly RSACryptoServiceProvider _pubKey;
    private LoginInfo? _login;

    #endregion

    #region Constructors

    public ConnectionImpl(Openbus bus) {
      _bus = bus;
      _certificate = Crypto.NewCertificate();
      _prvKey = Crypto.GetPrivateKey(_certificate);
      _pubKey = Crypto.GetPublicKey(_certificate);
      //TODO: Adicionar cache de logins
    }

    #endregion

//TODO: TRADUZIR MENSAGENS PARA INGLES

    #region IConnection Members

    void IConnection.LoginByPassword(string entity, byte[] password) {
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
      String id = _bus.Acs.loginByPassword(entity, pubBlob, encrypted, out lease);
      _login = new LoginInfo(id, entity);
    }

    void IConnection.LoginByCertificate(string entity, byte[] privKey) {
      if (IsLoggedIn()) {
        throw new ConnectionAlreadyLoggedIn();
      }

      Codec codec = GetCodec();

      byte[] challenge;
      LoginByCertificate loginByCert = _bus.Acs.startLoginByCertificate(entity,
                                                                        out
                                                                          challenge);
      byte[] answer = Crypto.Decrypt(_bus.BusKey, challenge);

      byte[] encrypted;
      byte[] pubBlob = _pubKey.ExportCspBlob(false);
      try {
        //encode answer and hash of public key
        LoginAuthenticationInfo info = new LoginAuthenticationInfo {
                                                                     data =
                                                                       answer,
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
      String id = loginByCert.login(pubBlob, encrypted, out lease);
      _login = new LoginInfo(id, entity);
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