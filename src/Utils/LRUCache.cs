using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace OpenbusAPI.Utils
{
  /// <summary>
  /// Representa uma Cache utilizando o algoritmo Least-Recently-Used (LRU).  
  /// </summary>
  /// <typeparam name="T">O tipo do elemento que será armezanado.</typeparam>
  internal class LRUCache<T> : ICollection<T>
  {
    #region Fields

    /// <summary>
    /// A estrutura onde a LRU estará armazenada.
    /// </summary>
    private LinkedList<T> cache;

    /// <summary>
    /// A capacidade total da LRU.
    /// </summary>
    private int capacity;

    /// <summary>
    /// Responsável pelo mecanismo de lock para sincronismo de acesso.
    /// </summary>
    private readonly object locker;

    #endregion

    #region Constructors

    /// <summary>
    /// Construtor.
    /// </summary>
    /// <param name="capacity">A capacidade da LRU.</param>
    public LRUCache(int capacity) {
      this.cache = new LinkedList<T>();
      this.capacity = capacity;
      this.locker = new Object();
    }

    #endregion

    #region ICollection<T> Members

    /// <inheritdoc />
    public int Count {
      get { return cache.Count; }
    }

    /// <inheritdoc />
    public bool IsReadOnly {
      get { return false; }
    }

    /// <inheritdoc />
    public void Add(T item) {
      if (item == null) {
        throw new ArgumentNullException("item");
      }
      if (this.Contains(item)) {
        return;
      }

      lock (locker) {
        if (cache.Count == capacity) {
          cache.RemoveFirst();
        }
        cache.AddLast(item);
      }
    }

    /// <inheritdoc />
    public void Clear() {
      cache.Clear();
    }

    /// <inheritdoc />
    public bool Contains(T item) {
      LinkedListNode<T> node = cache.Find(item);
      if (node == null) {
        return false;
      }
      T value = node.Value;

      lock (locker) {
        cache.Remove(node);
        cache.AddLast(node);
      }
      return true;
    }

    /// <inheritdoc />
    public void CopyTo(T[] array, int arrayIndex) {
      if (array == null) {
        throw new ArgumentNullException("array");
      }
      if (arrayIndex < 0) {
        throw new ArgumentOutOfRangeException("arrayIndex");
      }
      if (array.Length < (cache.Count - arrayIndex)) {
        throw new ArgumentException(
            "O tamanho do array não é suficiente para a quantidade de elementos");
      }

      cache.CopyTo(array, arrayIndex);
    }

    /// <inheritdoc />
    public bool Remove(T item) {
      if (item == null) {
        throw new ArgumentNullException("item");
      }

      lock (locker) {
        return cache.Remove(item);
      }
    }

    #endregion

    #region IEnumerable<T> Members

    public IEnumerator<T> GetEnumerator() {
      return cache.GetEnumerator();
    }

    #endregion

    #region IEnumerable Members

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() {
      return cache.GetEnumerator();
    }

    #endregion

    #region Public Members

    /// <inheritdoc />
    public override String ToString() {
      StringBuilder value = new StringBuilder();
      foreach (T element in cache) {
        value.Append(element.ToString() + " ");
      }
      return value.ToString();
    }

    #endregion
  }
}
