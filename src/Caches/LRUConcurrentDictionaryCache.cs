using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
      value = default(TValue);
      return false;
    }

    /// <summary>
    /// Define o valor de uma entrada na cache LRU e marca como recentemente usado.
    /// Caso já exista uma entrada com a mesma chave na cache, o valor antigo será removido e o valor atualizado.
    /// </summary>
    /// <param name="key">Chave associada ao valor</param>
    /// <param name="value">Valor a ser inserido ou atualizado</param>
    public void Set(TKey key, TValue value) {
      _lock.EnterWriteLock();
      try {
        _list.Remove(key);
        TValue old;
        _dictionary.TryRemove(key, out old);
        if (_dictionary.Count == MaxSize) {
          // remove entrada mais antiga do cache
          TValue removed;
          TKey firstKey = _list.First.Value;
          _dictionary.TryRemove(firstKey, out removed);
          _list.RemoveFirst();
        }
        _dictionary.TryAdd(key, value);
        _list.AddLast(key);
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
    /// Remove todas as entradas com chave pertencente a um conjunto de chaves.
    /// </summary>
    /// <param name="keys">Chaves das entradas a serem removidas.</param>
    public void RemoveEntriesWithKeys(IEnumerable<TKey> keys) {
      _lock.EnterWriteLock();
      try {
        foreach (TKey key in keys) {
          _list.Remove(key);
          TValue temp;
          _dictionary.TryRemove(key, out temp);
        }
      }
      finally {
        _lock.ExitWriteLock();
      }
    }

    /// <summary>
    /// Remove todas as entradas com valor pertencente a um conjunto de valores.
    /// </summary>
    /// <param name="values">Valores das entradas a serem removidas.</param>
    /// <returns>Lista com as chaves removidas.</returns>
    public IEnumerable<TKey> RemoveEntriesWithValues(IEnumerable<TValue> values) {
      IEnumerable<TKey> keys = new List<TKey>();
      _lock.EnterWriteLock();
      try {
        foreach (TValue value in values) {
          keys = keys.Union(RemoveEntriesWithValue(value));
        }
      }
      finally {
        _lock.ExitWriteLock();
      }
      return keys;
    }

    // NÃO É THREAD-SAFE!! Se mudar para public no futuro tem que tornar!
    private IEnumerable<TKey> RemoveEntriesWithValue(TValue value) {
      IEnumerable<TKey> removableKeys =
        _dictionary.Where(item => item.Value.Equals(value))
          .Select(item => item.Key).ToList();
      foreach (TKey key in removableKeys) {
        _list.Remove(key);
        TValue temp;
        _dictionary.TryRemove(key, out temp);
      }
      return removableKeys;
    }

    /// <summary>
    /// Informa o tamanho atual da cache.
    /// </summary>
    /// <returns>O tamanho atual da cache.</returns>
    public int GetSize() {
      _lock.EnterReadLock();
      try {
        return _list.Count;
      }
      finally {
        _lock.ExitReadLock();
      }
    }
  }
}