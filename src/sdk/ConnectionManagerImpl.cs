using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using log4net;
using omg.org.CORBA;
using omg.org.PortableInterceptor;
using tecgraf.openbus.sdk.exceptions;
using Current = omg.org.PortableInterceptor.Current;

namespace tecgraf.openbus.sdk {
  internal class ConnectionManagerImpl : ConnectionManager {
    #region Fields

    private static readonly ILog Logger =
      LogManager.GetLogger(typeof (ConnectionManagerImpl));

    /** Mapa de conexão que trata requisições de entrada por barramento */
    private readonly ConcurrentDictionary<String, Connection> _incomingDispatcherConn;
    /** Mapa de conexão por thread */
    private readonly ConcurrentDictionary<int, Connection> _connectedThreads;
    /** Threads com interceptação ignorada */
    private readonly ConditionalWeakTable<Thread, string> _ignoredThreads;

    private readonly ORB _orb;

    private readonly LoginCache _loginsCache;

    private readonly bool _legacy;

    /// <summary> Identificador do slot de thread corrente.</summary>
    internal int CurrentThreadSlotId { get; private set; }

    #endregion

    #region ConnectionManager methods

    public ConnectionManagerImpl(int currentThreadSlotId, bool legacySupport) {
      _connectedThreads = new ConcurrentDictionary<int, Connection>();
      _incomingDispatcherConn = new ConcurrentDictionary<string, Connection>();
      _ignoredThreads = new ConditionalWeakTable<Thread, string>();
      _loginsCache = new LoginCache();
      CurrentThreadSlotId = currentThreadSlotId;
      _legacy = legacySupport;
      _orb = OrbServices.GetSingleton();
    }

    public ORB ORB {
      get { return _orb; }
    }

    public Connection DefaultConnection { get; set; }

    public Connection CreateConnection(string host, short port) {
      IgnoreCurrentThread();
      try {
        ConnectionImpl conn = new ConnectionImpl(host, port, this, _legacy);
        conn.SetLoginsCache(_loginsCache);
        return conn;
      }
      finally {
        UnignoreCurrentThread();
      }
    }

    public Connection ThreadRequester {
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
        return connection ?? DefaultConnection;
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

    public void SetupBusDispatcher(Connection conn) {
      lock (_incomingDispatcherConn) {
        Connection removed;
        _incomingDispatcherConn.TryRemove(conn.BusId, out removed);
        _incomingDispatcherConn.TryAdd(conn.BusId, conn);
      }
    }

    public Connection GetBusDispatcher(string busId) {
      Connection incoming;
      _incomingDispatcherConn.TryGetValue(busId, out incoming);
      return incoming ?? DefaultConnection;
    }

    public Connection RemoveBusDispatcher(string busId) {
      Connection incoming;
      _incomingDispatcherConn.TryRemove(busId, out incoming);
      return incoming;
    }

    #endregion

    internal ICollection<Connection> GetIncomingConnections() {
      return _incomingDispatcherConn.Values;
    }

    internal void SetConnectionByThreadId(int threadId, Connection conn) {
      lock (_connectedThreads) {
        Connection removed;
        _connectedThreads.TryRemove(threadId, out removed);
        _connectedThreads.TryAdd(threadId, conn);
      }
    }

    internal Connection GetConnectionByThreadId(int threadId) {
      Connection conn;
      _connectedThreads.TryGetValue(threadId, out conn);
      return conn ?? DefaultConnection;
    }

    internal string DiscoverCredentialBus(AnyCredential anyCredential) {
      if (!anyCredential.IsLegacy) {
        return anyCredential.Credential.bus;
      }
      throw new NotImplementedException();
      /*
      foreach (KeyValuePair<string, ConcurrentBag<Connection>> keyValuePair in _buses) {
        foreach (ConnectionImpl connection in keyValuePair.Value) {
          //TODO: usar faceta legada da conexão para checar
          throw new NotImplementedException();
          //if (connection.Faceta.Validate()) {
          //  return connection.BusId;
          //}
          // só precisa checar através de uma conexão
          break;
        }
      }
      return String.Empty;
       */
    }

    internal void IgnoreCurrentThread() {
      lock (_ignoredThreads) {
        _ignoredThreads.Remove(Thread.CurrentThread);
        _ignoredThreads.Add(Thread.CurrentThread, String.Empty);
      }
    }

    internal void UnignoreCurrentThread() {
      _ignoredThreads.Remove(Thread.CurrentThread);
    }

    internal bool IsCurrentThreadIgnored() {
      string s;
      return _ignoredThreads.TryGetValue(Thread.CurrentThread, out s);
    }
  }
}