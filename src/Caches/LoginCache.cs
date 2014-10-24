using System;
using System.Threading;
using Org.BouncyCastle.Crypto;
using log4net;
using tecgraf.openbus.core.v2_1.credential;
using tecgraf.openbus.core.v2_1.services.access_control;
using tecgraf.openbus.security;

namespace tecgraf.openbus.caches {
  internal class LoginCache {
    private readonly ConnectionImpl _conn;

    private static readonly ILog Logger =
      LogManager.GetLogger(typeof (LoginCache));

    private readonly LRUConcurrentDictionaryCache<string, LoginEntry> _logins;
    private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

    public LoginCache(ConnectionImpl conn,
                      int maxSize =
                        LRUConcurrentDictionaryCache<string, LoginEntry>.
                        DefaultSize) {
      _conn = conn;
      _logins = new LRUConcurrentDictionaryCache<string, LoginEntry>(maxSize);
    }

    public LoginEntry GetLoginEntry(string loginId) {
      LoginEntry entry;
      if (!_logins.TryGetValue(loginId, out entry)) {
        Logger.Debug(
          String.Format(
            "Login {0} não foi encontrado na cache, obtendo informações.",
            loginId));
        // chamada remota, tem que ficar fora de qualquer lock
        byte[] pubKey;
        LoginInfo info;
        try {
          SignedData signed;
          info = _conn.LoginRegistry.getLoginInfo(loginId, out signed);
          if (!Crypto.VerifySignature(_conn.BusKey, signed.encoded, signed.signature)) {
            throw new InvalidPublicKey {
              message = "Hash signature doesn't match."
            };
          }
          pubKey = signed.encoded;
        }
        catch (InvalidLogins) {
          return null;
        }
        _lock.EnterWriteLock();
        try {
          // Checa novamente para ver se algo mudou após a finalização da
          // chamada remota. A informação na cache deve ser mais confiável
          // pois provavelmente é mais antiga, e assim o "deadline" deve ser
          // mais acurado do que o que seria gerado agora.
          if (!_logins.TryGetValue(loginId, out entry)) {
            entry = new LoginEntry {
                                     Publickey =
                                       Crypto.CreatePublicKeyFromBytes(pubKey),
                                     EncodedKey = pubKey,
                                     LoginId = info.id,
                                     Entity = info.entity,
                                     DeadLine = DateTime.Now.Ticks
                                   };
            _logins.TryAdd(loginId, entry);
            Logger.Debug(String.Format(
              "Login {0} válido e adicionado à cache.", loginId));
            return entry.Clone();
          }
        }
        finally {
          _lock.ExitWriteLock();
        }
      }
      Logger.Debug(
        String.Format("Login {0} está na cache, procedendo à validação.",
                      loginId));
      // valida o login
      long deadline;
      _lock.EnterReadLock();
      try {
        deadline = entry.DeadLine;
      }
      finally {
        _lock.ExitReadLock();
      }
      if (deadline < DateTime.Now.Ticks) {
          int validity = _conn.LoginRegistry.getLoginValidity(loginId);
        if (validity <= 0) {
          // login inválido
          Logger.Debug(String.Format("Login {0} é inválido.", loginId));
          return null;
        }
        _lock.EnterWriteLock();
        try {
          entry.DeadLine = DateTime.Now.Ticks + validity;
        }
        finally {
          _lock.ExitWriteLock();
        }
      }
      Logger.Debug(String.Format("Login {0} é válido.", loginId));
      return entry.Clone();
    }

    internal void UpdateEntry(LoginEntry entry) {
      _lock.EnterWriteLock();
      try {
        LoginEntry cacheEntry;
        if (_logins.TryGetValue(entry.LoginId, out cacheEntry)) {
          cacheEntry.Update(entry);
        }
      }
      finally {
        _lock.ExitWriteLock();
      }
    }

    internal class LoginEntry {
      public String LoginId;
      public String Entity;
      public int Validity;
      public long DeadLine;
      public AsymmetricKeyParameter Publickey;
      public byte[] EncodedKey;

      public LoginEntry Clone() {
        LoginEntry ret = new LoginEntry();
        lock (this) {
          ret.LoginId = string.Copy(LoginId);
          ret.Entity = string.Copy(Entity);
          ret.Validity = Validity;
          ret.DeadLine = DeadLine;
          ret.EncodedKey = new byte[EncodedKey.Length];
          Array.Copy(EncodedKey, ret.EncodedKey, EncodedKey.Length);
          ret.Publickey = Crypto.CreatePublicKeyFromBytes(EncodedKey);
        }
        return ret;
      }

      public void Update(LoginEntry another) {
        lock (this) {
          LoginId = string.Copy(another.LoginId);
          Entity = string.Copy(another.Entity);
          Validity = another.Validity;
          DeadLine = another.DeadLine;
          EncodedKey = new byte[another.EncodedKey.Length];
          Array.Copy(another.EncodedKey, EncodedKey, another.EncodedKey.Length);
          Publickey = Crypto.CreatePublicKeyFromBytes(another.EncodedKey);
        }
      }
    }
  }
}