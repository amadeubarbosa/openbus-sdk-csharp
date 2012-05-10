using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using log4net;

namespace tecgraf.openbus.caches {
  internal class LRUConcurrentDictionaryCache<TKey, TValue> {
    private readonly ILog _logger =
      LogManager.GetLogger(typeof (LRUConcurrentDictionaryCache<TKey, TValue>));

    private readonly LinkedList<TKey> _list;
    private readonly ConcurrentDictionary<TKey, TValue> _dictionary;

    private const int DefaultSize = 1024;
    private TValue _default;

    public LRUConcurrentDictionaryCache() : this(DefaultSize) {
    }

    public LRUConcurrentDictionaryCache(int maxSize) {
      MaxSize = maxSize <= 0 ? DefaultSize : maxSize;
      _list = new LinkedList<TKey>();
      _dictionary = new ConcurrentDictionary<TKey, TValue>();
    }

    public int MaxSize { get; private set; }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public bool TryGetValue(TKey key, out TValue value) {
      if (_dictionary.TryGetValue(key, out value)) {
        _list.Remove(key);
        _list.AddLast(key);
        return true;
      }
      value = _default;
      return false;
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public bool TryAdd(TKey key, TValue value) {
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

    [MethodImpl(MethodImplOptions.Synchronized)]
    public void Clear() {
      _dictionary.Clear();
      _list.Clear();
    }

    /// <summary>
    /// Remove a entrada mais antiga da cache.
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.Synchronized)]
    private bool TryRemoveOldest(out TValue value) {
      TKey firstKey = _list.First.Value;
      if (_dictionary.TryRemove(firstKey, out value)) {
        _list.RemoveFirst();
        return true;
      }
      value = _default;
      return false;
    }
  }
}