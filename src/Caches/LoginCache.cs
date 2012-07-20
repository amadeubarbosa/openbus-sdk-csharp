using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Org.BouncyCastle.Crypto;
using log4net;
using omg.org.CORBA;
using tecgraf.openbus.core.v1_05.access_control_service;
using tecgraf.openbus.core.v2_0.services;
using tecgraf.openbus.core.v2_0.services.access_control;
using tecgraf.openbus.security;

namespace tecgraf.openbus.caches {
  internal class LoginCache {
    private static readonly ILog Logger =
      LogManager.GetLogger(typeof (LoginCache));

    // não uso cache LRU pois há uma política de remoção específica
    private readonly ConcurrentDictionary<string, LoginEntry> _logins;
    private readonly IsValidCache _legacyCache;

    public LoginCache() {
      _logins = new ConcurrentDictionary<string, LoginEntry>();
      _legacyCache = new IsValidCache();
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public bool ValidateLogin(AnyCredential anyCredential, ConnectionImpl conn) {
      if (anyCredential.IsLegacy) {
        return _legacyCache.IsValid(anyCredential.LegacyCredential, conn);
      }
      string loginId = anyCredential.Credential.login;
      long time = DateTime.Now.Ticks;
      LoginEntry entry;
      bool contains = false;
      if (_logins.TryGetValue(loginId, out entry)) {
        contains = true;
        long elapsed = (time - entry.LastTime) / TimeSpan.TicksPerSecond;
        if (elapsed <= (entry.Validity)) {
          // login é valido
          return true;
        }
      }
      string busid = conn.BusId;
      IList<string> ids = new List<String>();
      IList<LoginEntry> logins = new List<LoginEntry>(_logins.Values);
      foreach (LoginEntry lEntry in logins) {
        if (lEntry.BusId.Equals(busid)) {
          ids.Add(lEntry.LoginId);
        }
      }
      if (!contains) {
        ids.Add(loginId);
      }
      time = DateTime.Now.Ticks;
      string[] idsArray = new string[ids.Count];
      ids.CopyTo(idsArray, 0);
      conn.Manager.Requester = conn;
      int[] validities =
        conn.LoginRegistry.getValidity(idsArray);
      bool isValid = false;
      for (int i = 0; i < idsArray.Length; i++) {
        string id = idsArray[i];
        int validity = validities[i];
        if (validity > 0) {
          LoginEntry loginEntry;
          if (!_logins.TryGetValue(id, out loginEntry)) {
            loginEntry = new LoginEntry {
                                          LoginId = id,
                                          BusId = busid,
                                          LastTime = time,
                                          Validity = validity
                                        };
            _logins.TryAdd(id, loginEntry);
          }
          loginEntry.LastTime = time;
          loginEntry.Validity = validity;
          if (id == loginId) {
            isValid = true;
          }
        }
        else {
          LoginEntry removed;
          _logins.TryRemove(id, out removed);
        }
      }
      return isValid;
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public bool GetLoginEntity(String loginId, ConnectionImpl conn,
                               out string entity,
                               out AsymmetricKeyParameter pubkey) {
      byte[] key;
      LoginInfo info;
      LoginEntry entry;
      if (_logins.TryGetValue(loginId, out entry)) {
        if (entry.Entity != null) {
          entity = entry.Entity;
          pubkey = entry.Publickey;
          return true;
        }
        info = GetLoginInfo(conn, loginId, out key);
        entry.Entity = info.entity;
        entry.Publickey = Crypto.CreatePublicKeyFromBytes(key);
        entity = entry.Entity;
        pubkey = entry.Publickey;
        return true;
      }
      // A entrada pode ter sido validada e removida antes de entrar nesse
      // método. Obtém a entrada mas com validade 0 para que seja checada.
      entry = new LoginEntry();
      info = GetLoginInfo(conn, loginId, out key);
      entry.Publickey = Crypto.CreatePublicKeyFromBytes(key);
      entry.Entity = info.entity;
      entry.LoginId = info.id;
      entry.BusId = conn.BusId;
      entry.LastTime = DateTime.Now.Ticks;
      entry.Validity = 0;
      if (_logins.TryAdd(loginId, entry)) {
        entity = entry.Entity;
        pubkey = entry.Publickey;
        return true;
      }
      entity = null;
      pubkey = null;
      return false;
    }

    private LoginInfo GetLoginInfo(ConnectionImpl conn, string loginId,
                                   out byte[] key) {
      try {
        conn.Manager.Requester = conn;
        return conn.LoginRegistry.getLoginInfo(loginId, out key);
      }
      catch (InvalidLogins il) {
        Logger.Error(
          String.Format(
            "Erro InvalidLogins ao obter informações do login {0}.", loginId),
          il);
        throw new NO_PERMISSION(InvalidLoginCode.ConstVal,
                                CompletionStatus.Completed_No);
      }
      catch (ServiceFailure sf) {
        Logger.Error(
          String.Format(
            "Erro ServiceFailure ao obter informações do login {0}.", loginId),
          sf);
        throw new NO_PERMISSION(UnverifiedLoginCode.ConstVal,
                                CompletionStatus.Completed_No);
      }
    }

    /**
     * Valor do mapa de logins da cache.
     * 
     * @author Tecgraf
     */

    private class LoginEntry {
      public String BusId;
      public String LoginId;
      public String Entity;
      public int Validity;
      public long LastTime;
      public AsymmetricKeyParameter Publickey;
    }

    //TODO verificar se dá para fazer esse cache de uma forma melhor, de acordo com a implementação lua que o maia fez.
    private class IsValidCache {
      /**
       * O mapa da cache de logins.
       */
      private readonly LRUConcurrentDictionaryCache<IsValidKey, Boolean> _cache;

      public IsValidCache() {
        _cache = new LRUConcurrentDictionaryCache<IsValidKey, bool>();
      }

      public bool IsValid(Credential credential, ConnectionImpl conn) {
        IsValidKey key = new IsValidKey(credential.identifier, credential.owner);
        bool valid;
        _cache.TryGetValue(key, out valid);
        if (!valid) {
          if (conn.Legacy) {
            conn.Manager.Requester = conn;
            valid = conn.LegacyAccess.isValid(credential);
            _cache.TryAdd(key, valid);
          }
        }
        return valid;
      }

    }

    private class IsValidKey {
      private readonly string _id;
      private readonly string _owner;

      public IsValidKey(string id, string owner) {
        _id = id;
        _owner = owner;
      }

      public override int GetHashCode() {
        const int prime = 31;
        int result = 1;
        result = prime * result + GetOuterType().GetHashCode();
        result = prime * result + ((_id == null) ? 0 : _id.GetHashCode());
        result = prime * result + ((_owner == null) ? 0 : _owner.GetHashCode());
        return result;
      }

      public override bool Equals(Object obj) {
        if (ReferenceEquals(null, obj)) {
          return false;
        }
        if (ReferenceEquals(this, obj)) {
          return true;
        }

        IsValidKey other = (IsValidKey) obj;
        if (!(GetOuterType() == other.GetOuterType())) {
          return false;
        }
        if (_id == null) {
          if (other._id != null) {
            return false;
          }
        }
        else if (!_id.Equals(other._id)) {
          return false;
        }
        if (_owner == null) {
          if (other._owner != null) {
            return false;
          }
        }
        else if (!_owner.Equals(other._owner)) {
          return false;
        }
        return true;
      }

      private Type GetOuterType() {
        return GetType();
      }
    }
  }
}