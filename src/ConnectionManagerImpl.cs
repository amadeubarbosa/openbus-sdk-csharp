using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using log4net;
using omg.org.CORBA;
using omg.org.PortableInterceptor;
using tecgraf.openbus.caches;
using tecgraf.openbus.exceptions;
using Current = omg.org.PortableInterceptor.Current;

namespace tecgraf.openbus {
  internal class ConnectionManagerImpl : ConnectionManager {
    #region Fields

    private static readonly ILog Logger =
      LogManager.GetLogger(typeof (ConnectionManagerImpl));

    /** Mapa de conexão que trata requisições de entrada por barramento */

    private readonly ConcurrentDictionary<String, Connection>
      _incomingDispatcherConn;

    /** Mapa de conexão por thread */
    private readonly ConcurrentDictionary<int, Connection> _connectedThreads;

    private readonly ORB _orb;

    internal readonly LoginCache LoginsCache;

    private readonly bool _legacy;

    // Identificador do slot de thread corrente.
    internal int CurrentThreadSlotId { get; private set; }

    // Identificador do slot de interceptação ignorada.
    private readonly int _ignoreThreadSlotId;

    #endregion

    #region ConnectionManager methods

    public ConnectionManagerImpl(int currentThreadSlotId, int ignoreThreadSlotId, bool legacySupport) {
      _connectedThreads = new ConcurrentDictionary<int, Connection>();
      _incomingDispatcherConn = new ConcurrentDictionary<string, Connection>();
      LoginsCache = new LoginCache();
      CurrentThreadSlotId = currentThreadSlotId;
      _ignoreThreadSlotId = ignoreThreadSlotId;
      _legacy = legacySupport;
      _orb = OrbServices.GetSingleton();
    }

    public ORB ORB {
      get { return _orb; }
    }

    public Connection DefaultConnection { get; set; }

    public Connection CreateConnection(string host, ushort port) {
      IgnoreCurrentThread();
      try {
        ConnectionImpl conn = new ConnectionImpl(host, port, this, _legacy);
        conn.SetLoginsCache(LoginsCache);
        return conn;
      }
      finally {
        UnignoreCurrentThread();
      }
    }

    public Connection Requester {
      get {
        Current current = GetPICurrent();
        int id;
        try {
          Object obj = current.get_slot(CurrentThreadSlotId);
          if (obj == null) {
            return null;
          }
          id = Convert.ToInt32(obj);
        }
        catch (InvalidSlot e) {
          const string message =
            "Falha inesperada ao acessar o slot da thread corrente";
          Logger.Fatal(message, e);
          throw new OpenBusInternalException(message);
        }

        Connection connection;
        return _connectedThreads.TryGetValue(id, out connection) ? connection : null;
      }
      set {
        int id = Thread.CurrentThread.ManagedThreadId;
        Current current = GetPICurrent();
        try {
          current.set_slot(CurrentThreadSlotId, id);
        }
        catch (InvalidSlot e) {
          const string message =
            "Falha inesperada ao acessar o slot da thread corrente";
          Logger.Fatal(message, e);
          throw new OpenBusInternalException(message, e);
        }
        SetConnectionByThreadId(id, value);
      }
    }

    public void SetDispatcher(Connection conn) {
      if (conn == null) {
        throw new ArgumentNullException();
      }
      if ((!conn.Login.HasValue) || conn.BusId == null) {
        throw new NotLoggedInException();
      }
      lock (_incomingDispatcherConn) {
        Connection removed;
        _incomingDispatcherConn.TryRemove(conn.BusId, out removed);
        _incomingDispatcherConn.TryAdd(conn.BusId, conn);
      }
    }

    public Connection GetDispatcher(string busId) {
      Connection incoming;
      return _incomingDispatcherConn.TryGetValue(busId, out incoming) ? incoming : null;
    }

    public Connection ClearDispatcher(string busId) {
      Connection incoming;
      _incomingDispatcherConn.TryRemove(busId, out incoming);
      return incoming;
    }

    #endregion

    internal Current GetPICurrent() {
      Current current = ORB.resolve_initial_references("PICurrent") as Current;
      if (current == null) {
        const string message =
          "Falha inesperada ao acessar o slot da thread corrente";
        Logger.Fatal(message);
        throw new OpenBusInternalException(message);
      }
      return current;
    }

    internal IEnumerable<Connection> GetIncomingConnections() {
      IList<Connection> list = new List<Connection>(_incomingDispatcherConn.Values);
      if (DefaultConnection != null) {
        list.Add(DefaultConnection);
      }
      return list;
    }

    internal void SetConnectionByThreadId(int threadId, Connection conn) {
      lock (_connectedThreads) {
        Connection removed;
        _connectedThreads.TryRemove(threadId, out removed);
        if (conn != null) {
          _connectedThreads.TryAdd(threadId, conn);
        }
      }
    }

    internal Connection GetConnectionByThreadId(int threadId) {
      Connection conn;
      return _connectedThreads.TryGetValue(threadId, out conn) ? conn : null;
    }

    internal void IgnoreCurrentThread() {
      Current current = GetPICurrent();
      try {
        current.set_slot(_ignoreThreadSlotId, Boolean.TrueString);
      }
      catch (InvalidSlot e) {
        const string message =
          "Falha inesperada ao acessar o slot de interceptação ignorada.";
        Logger.Fatal(message, e);
        throw new OpenBusInternalException(message, e);
      }
    }

    internal void UnignoreCurrentThread() {
      Current current = GetPICurrent();
      try {
        current.set_slot(_ignoreThreadSlotId, Boolean.FalseString);
      }
      catch (InvalidSlot e) {
        const string message =
          "Falha inesperada ao acessar o slot de interceptação ignorada.";
        Logger.Fatal(message, e);
        throw new OpenBusInternalException(message, e);
      }
    }

    internal bool IsCurrentThreadIgnored(RequestInfo ri) {
      try {
        return Convert.ToBoolean(ri.get_slot(_ignoreThreadSlotId));
      }
      catch (InvalidSlot e) {
        const string message =
          "Falha inesperada ao acessar o slot do login corrente";
        Logger.Fatal(message, e);
        throw new OpenBusInternalException(message);
      }
    }
  }
}