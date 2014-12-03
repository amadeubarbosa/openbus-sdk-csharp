using System;
using System.Threading;
using Org.BouncyCastle.Crypto;
using log4net;
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
        byte[] keyBytes;
        AsymmetricKeyParameter pubKey;
        LoginInfo info;
        try {
          info = _conn.LoginRegistry.getLoginInfo(loginId, out keyBytes);
          pubKey = Crypto.CreatePublicKeyFromBytes(keyBytes);
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
                                     Publickey = pubKey,
                                     KeyBytes = keyBytes,
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
      public byte[] KeyBytes;

      public LoginEntry Clone() {
        LoginEntry ret = new LoginEntry();
        lock (this) {
          ret.LoginId = string.Copy(LoginId);
          ret.Entity = string.Copy(Entity);
          ret.Validity = Validity;
          ret.DeadLine = DeadLine;
          ret.KeyBytes = new byte[KeyBytes.Length];
          Array.Copy(KeyBytes, ret.KeyBytes, KeyBytes.Length);
          ret.Publickey = Crypto.CreatePublicKeyFromBytes(KeyBytes);
        }
        return ret;
      }

      public void Update(LoginEntry another) {
        lock (this) {
          LoginId = string.Copy(another.LoginId);
          Entity = string.Copy(another.Entity);
          Validity = another.Validity;
          DeadLine = another.DeadLine;
          KeyBytes = new byte[another.KeyBytes.Length];
          Array.Copy(another.KeyBytes, KeyBytes, another.KeyBytes.Length);
          Publickey = Crypto.CreatePublicKeyFromBytes(another.KeyBytes);
        }
      }
    }
  }
}