using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using log4net;
using omg.org.CORBA;
using omg.org.PortableInterceptor;
using tecgraf.openbus.sdk.exceptions;
using Current = omg.org.PortableInterceptor.Current;

namespace tecgraf.openbus.sdk.multiplexed {
  internal class ConnectionMultiplexerImpl : ConnectionMultiplexer {
    #region Fields

    private static readonly ILog Logger =
      LogManager.GetLogger(typeof (ConnectionMultiplexerImpl));

    /// <summary> Conexões por barramento.</summary>
    private readonly ConcurrentDictionary<string, ConcurrentBag<Connection>> _buses;

    /// <summary> Mapa de conexão default por barramento.</summary>
    private readonly ConcurrentDictionary<string, Connection> _busDefaultConn;

    /// <summary> Mapa de conexão por thread.</summary>
    private readonly ConcurrentDictionary<int, Connection> _connectedThreads;

    private readonly ORB _orb;

    /// <summary> Identificador do slot de thread corrente.</summary>
    public int CurrentThreadSlotId { get; private set; }

    #endregion

    #region ConnectionMultiplexer methods

    public ConnectionMultiplexerImpl(int currentThreadSlotId) {
      _buses = new ConcurrentDictionary<string, ConcurrentBag<Connection>>();
      _busDefaultConn = new ConcurrentDictionary<string, Connection>();
      _connectedThreads = new ConcurrentDictionary<int, Connection>();
      CurrentThreadSlotId = currentThreadSlotId;
      _orb = OrbServices.GetSingleton();
    }

    public Connection CurrentConnection {
      get {
        const string message =
          "Falha inesperada ao acessar o slot da thread corrente";
        Current current =
          _orb.resolve_initial_references("PICurrent") as Current;
        if (current == null) {
          Logger.Fatal(message);
          throw new OpenBusException(message);
        }
        int id;
        try {
          id = (int) current.get_slot(CurrentThreadSlotId);
        }
        catch (InvalidSlot e) {
          Logger.Fatal(message, e);
          throw new OpenBusException(message);
        }

        Connection connection;
        _connectedThreads.TryGetValue(id, out connection);
        // Todo: deixo retornar null ou lanço algum tipo de exceção?
        return connection;
      }
      set {
        const string message =
          "Falha inesperada ao acessar o slot da thread corrente";
        int id = Thread.CurrentThread.ManagedThreadId;
        Current current =
          _orb.resolve_initial_references("PICurrent") as Current;
        if (current == null) {
          Logger.Fatal(message);
          throw new OpenBusException(message);
        }
        try {
          current.set_slot(CurrentThreadSlotId, id);
        }
        catch (InvalidSlot e) {
          Logger.Fatal(message, e);
          throw new OpenBusException(message, e);
        }
        SetConnectionByThreadId(id, value);
      }
    }

    public void SetIncomingConnection(string busId, Connection conn) {
      lock (_busDefaultConn) {
        Connection removed;
        _busDefaultConn.TryRemove(busId, out removed);
        if (conn != null) {
          _busDefaultConn.TryAdd(busId, conn);
        }
      }
    }

    public Connection GetIncomingConnection(string busId) {
      Connection incoming;
      _busDefaultConn.TryGetValue(busId, out incoming);
      return incoming;
    }

    #endregion

    private void SetConnectionByThreadId(int threadId, Connection conn) {
      lock (_connectedThreads) {
        Connection removed;
        _connectedThreads.TryRemove(threadId, out removed);
        if (conn != null) {
          _connectedThreads.TryAdd(threadId, conn);
        }
      }
    }

    public Connection GetConnectionByThreadId(int threadId) {
      Connection conn;
      _connectedThreads.TryGetValue(threadId, out conn);
      return conn;
    }

    public bool HasBus(string busid) {
      return _buses.ContainsKey(busid);
    }

    public void AddConnection(Connection conn) {
    lock (_buses) {
      ConcurrentBag<Connection> bag;
      if (!_buses.TryGetValue(conn.BusId, out bag)) {
        bag = new ConcurrentBag<Connection>();
        _buses.TryAdd(conn.BusId, bag);
      }
      bag.Add(conn);
    }
  }

    public void RemoveConnection(Connection conn) {
      string busId = conn.BusId;
      // mapa de conexões por barramentos
      lock (_buses) {
        ConcurrentBag<Connection> bag;
        if (_buses.TryGetValue(busId, out bag)) {
          bag.TryTake(out conn);
        }
      }
      // mapa de conexão default por barramento
      lock (_busDefaultConn) {
        Connection defconn;
        if (_busDefaultConn.TryGetValue(busId, out defconn)) {
          if (ReferenceEquals(defconn, conn)) {
            Connection removed;
            _busDefaultConn.TryRemove(busId, out removed);
          }
        }
      }
      // mapa de conexão por thread
      IList<int> toRemove = new List<int>();
      lock (_connectedThreads) {
        foreach (KeyValuePair<int, Connection> entry in _connectedThreads) {
          if (ReferenceEquals(entry.Value, conn)) {
            toRemove.Add(entry.Key);
          }
        }
        foreach (int id in toRemove) {
          Connection removed;
          _connectedThreads.TryRemove(id, out removed);
        }
      }
    }
  }
}