using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using log4net;

namespace tecgraf.openbus.caches {
  internal class LRUConcurrentDictionaryCache<TKey, TValue> {
    private readonly ILog _logger =
      LogManager.GetLogger(typeof (LRUConcurrentDictionaryCache<TKey, TValue>));

    private readonly LinkedList<TKey> _list;
    private readonly ConcurrentDictionary<TKey, TValue> _dictionary;
    private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

    internal const int DefaultSize = 1024;
    private TValue _default;

    public LRUConcurrentDictionaryCache() : this(DefaultSize) {
    }

    public LRUConcurrentDictionaryCache(int maxSize) {
      MaxSize = maxSize <= 0 ? DefaultSize : maxSize;
      _list = new LinkedList<TKey>();
      _dictionary = new ConcurrentDictionary<TKey, TValue>();
    }

    public int MaxSize { get; private set; }

    public bool TryGetValue(TKey key, out TValue value) {
      _lock.EnterUpgradeableReadLock();
      try {
        if (_dictionary.TryGetValue(key, out value)) {
          _lock.EnterWriteLock();
          try {
            _list.Remove(key);
            _list.AddLast(key);
          }
          finally {
            _lock.ExitWriteLock();
          }
          return true;
        }
      }
      finally {
        _lock.ExitUpgradeableReadLock();
      }
      value = _default;
      return false;
    }

    public bool TryAdd(TKey key, TValue value) {
      _lock.EnterWriteLock();
      try {
        if (_dictionary.Count >= MaxSize) {
          if (_dictionary.Count == MaxSize) {
            TValue removed;
            if (!TryRemoveOldest(out removed)) {
              return false;
            }
          }
          else {
            // erro de thread, não deveria entrar aqui.
            _logger.Fatal(
              "Erro de consistência no dicionário LRU. Limpando a cache.");
            Clear();
            return false;
          }
        }
        if (_dictionary.TryAdd(key, value)) {
          _list.AddLast(key);
          return true;
        }
        return false;
      }
      finally {
        _lock.ExitWriteLock();
      }
    }

    public void Clear() {
      _lock.EnterWriteLock();
      try {
        _dictionary.Clear();
        _list.Clear();
      }
      finally {
        _lock.ExitWriteLock();
      }
    }

    /// <summary>
    /// Remove a entrada mais antiga da cache.
    /// </summary>
    /// <returns></returns>
    private bool TryRemoveOldest(out TValue value) {
      _lock.EnterWriteLock();
      try {
        TKey firstKey = _list.First.Value;
        if (_dictionary.TryRemove(firstKey, out value)) {
          _list.RemoveFirst();
          return true;
        }
      }
      finally {
        _lock.ExitWriteLock();
      }
      value = _default;
      return false;
    }
  }
}