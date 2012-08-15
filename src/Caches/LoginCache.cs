using System;
using System.Threading;
using Org.BouncyCastle.Crypto;
using log4net;
using tecgraf.openbus.core.v2_0.services.access_control;
using tecgraf.openbus.security;

namespace tecgraf.openbus.caches {
  internal class LoginCache {
    private static readonly ILog Logger =
      LogManager.GetLogger(typeof (LoginCache));

    private readonly LRUConcurrentDictionaryCache<string, LoginEntry> _logins;
    private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

    private readonly LoginRegistry _lr;

    public LoginCache(LoginRegistry lr,
                      int maxSize =
                        LRUConcurrentDictionaryCache<string, LoginEntry>.
                        DefaultSize) {
      _logins = new LRUConcurrentDictionaryCache<string, LoginEntry>(maxSize);
      _lr = lr;
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
          info = _lr.getLoginInfo(loginId, out pubKey);
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
        int validity = _lr.getValidity(new[] {loginId})[0];
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

    internal void UpdateEntryAllowLegacyDelegate(LoginEntry entry) {
      _lock.EnterWriteLock();
      try {
        LoginEntry cacheEntry;
        if (_logins.TryGetValue(entry.LoginId, out cacheEntry)) {
          cacheEntry.UpdateAllowLegacyDelegate(entry.AllowLegacyDelegate);
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
      public bool? AllowLegacyDelegate;

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
          if (AllowLegacyDelegate.HasValue) {
            ret.AllowLegacyDelegate = AllowLegacyDelegate.Value;
          }
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
          if (another.AllowLegacyDelegate.HasValue) {
            AllowLegacyDelegate = another.AllowLegacyDelegate.Value;
          }
        }
      }

      public void UpdateAllowLegacyDelegate(bool? allow) {
        lock (this) {
          if (allow.HasValue) {
            AllowLegacyDelegate = allow.Value;
          }
          else {
            AllowLegacyDelegate = null;
          }
        }
      }
    }
  }
}