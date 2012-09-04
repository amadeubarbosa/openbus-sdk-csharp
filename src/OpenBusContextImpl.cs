using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Threading;
using log4net;
using omg.org.CORBA;
using omg.org.PortableInterceptor;
using tecgraf.openbus.core.v2_0.services.access_control;
using tecgraf.openbus.core.v2_0.services.offer_registry;
using tecgraf.openbus.exceptions;
using tecgraf.openbus.security;
using Current = omg.org.PortableInterceptor.Current;

namespace tecgraf.openbus {
  internal class OpenBusContextImpl : OpenBusContext {
    #region Fields

    private static readonly ILog Logger =
      LogManager.GetLogger(typeof (OpenBusContextImpl));

    // Mapa de conexões
    private readonly ConcurrentDictionary<Guid, Connection> _connections;

    private readonly ORB _orb;

    // Identificador do slot de id de conexão corrente.
    private readonly int _connectionIdSlotId;

    // Identificador do slot de interceptação ignorada.
    private readonly int _ignoreThreadSlotId;

    private readonly int _joinedChainSlotId;
    private readonly int _chainSlotId;

    private Connection _defaultConnection;
    private CallDispatchCallback _onCallDispatchCallback;
    //TODO rever se posso dividir em locks diferentes
    private readonly ReaderWriterLockSlim _lock;

    private const string ConnectionIdErrorMsg =
      "Falha inesperada ao acessar o slot do id de conexão corrente";

    #endregion

    #region Constructors

    public OpenBusContextImpl(int connectionIdSlotId, int ignoreThreadSlotId,
                              int joinedChainSlotId, int chainSlotId) {
      _connections = new ConcurrentDictionary<Guid, Connection>();
      _connectionIdSlotId = connectionIdSlotId;
      _ignoreThreadSlotId = ignoreThreadSlotId;
      _joinedChainSlotId = joinedChainSlotId;
      _chainSlotId = chainSlotId;
      _orb = OrbServices.GetSingleton();
      _lock = new ReaderWriterLockSlim();
    }

    #endregion

    #region OpenBusContext methods

    public ORB ORB {
      get { return _orb; }
    }

    public CallDispatchCallback OnCallDispatch {
      get {
        _lock.EnterReadLock();
        try {
          return _onCallDispatchCallback;
        }
        finally {
          _lock.ExitReadLock();
        }
      }
      set {
        _lock.EnterWriteLock();
        try {
          _onCallDispatchCallback = value;
        }
        finally {
          _lock.ExitWriteLock();
        }
      }
    }

    public Connection SetDefaultConnection(Connection conn) {
      Connection previous = GetDefaultConnection();
      _lock.EnterWriteLock();
      try {
        _defaultConnection = conn;
      }
      finally {
        _lock.ExitWriteLock();
      }
      return previous;
    }

    public Connection GetDefaultConnection() {
      _lock.EnterReadLock();
      try {
        return _defaultConnection;
      }
      finally {
        _lock.ExitReadLock();
      }
    }

    public Connection CreateConnection(string host, ushort port,
                                       ConnectionProperties props) {
      if (host == null || host.Equals(string.Empty)) {
        throw new ArgumentException("Endereço inválido.");
      }
      if (port == 0) {
        throw new ArgumentException("Porta inválida.");
      }
      IgnoreCurrentThread();
      try {
        bool legacyDisable = false;
        bool originator = false;
        PrivateKeyImpl accessKey = null;
        if (props != null) {
          if (props.LegacyDisable != ConnectionPropertiesImpl.LegacyDisableDefault) {
            legacyDisable = props.LegacyDisable;
            LogPropertyChanged(ConnectionPropertiesImpl.LegacyDisableProperty,
                               legacyDisable.ToString(CultureInfo.InvariantCulture));
          }
          if (!legacyDisable) {
            if (
              props.LegacyDelegate.Equals(
                ConnectionPropertiesImpl.LegacyDelegateOriginatorOption)) {
              originator = true;
              LogPropertyChanged(ConnectionPropertiesImpl.LegacyDelegateProperty,
                                 props.LegacyDelegate);
            }
          }
          if (props.AccessKey != null) {
            accessKey = (PrivateKeyImpl)props.AccessKey;
            LogPropertyChanged(ConnectionPropertiesImpl.AccessKeyProperty,
                               "{AccessKey provida pelo usuário}");
          }
        }
        return new ConnectionImpl(host, port, this, !legacyDisable, originator,
                                  accessKey);
      }
      finally {
        UnignoreCurrentThread();
      }
    }

    public Connection GetCurrentConnection() {
      try {
        Guid? guid = GetPICurrent().get_slot(_connectionIdSlotId) as Guid?;
        if (!guid.HasValue) {
          return null;
        }
        Connection connection = GetConnectionById(guid.Value);
        return connection;
      }
      catch (InvalidSlot e) {
        Logger.Fatal(ConnectionIdErrorMsg, e);
        throw;
      }
    }

    public Connection SetCurrentConnection(Connection conn) {
      Connection previous = null;
      Current current = GetPICurrent();
      try {
        // tenta reaproveitar o guid
        Guid? guid = current.get_slot(_connectionIdSlotId) as Guid?;
        if (guid.HasValue) {
          previous = GetConnectionById(guid.Value);
          if (conn == null) {
            current.set_slot(_connectionIdSlotId, null);
            SetConnectionById(guid.Value, null);
            return previous;
          }
        }
        else {
          if (conn == null) {
            return null;
          }
          guid = Guid.NewGuid();
          current.set_slot(_connectionIdSlotId, guid);
        }
        SetConnectionById(guid.Value, conn);
        return previous;
      }
      catch (InvalidSlot e) {
        Logger.Fatal(ConnectionIdErrorMsg, e);
        throw;
      }
    }

    public void JoinChain(CallerChain chain) {
      if (chain == null) {
        chain = CallerChain;
      }
      Current current = GetPICurrent();
      try {
        current.set_slot(_joinedChainSlotId, chain);
      }
      catch (InvalidSlot e) {
        Logger.Fatal("Falha inesperada ao acessar o slot da joined chain.", e);
        throw;
      }
    }

    public CallerChain CallerChain {
      get {
        Current current = GetPICurrent();
        try {
          return (CallerChainImpl) current.get_slot(_chainSlotId);
        }
        catch (InvalidSlot e) {
          Logger.Fatal(
            "Falha inesperada ao acessar o slot da cadeia corrente.", e);
          throw;
        }
      }
    }

    public void ExitChain() {
      Current current = GetPICurrent();
      try {
        current.set_slot(_joinedChainSlotId, null);
      }
      catch (InvalidSlot e) {
        Logger.Fatal("Falha inesperada ao acessar o slot da joined chain.", e);
        throw;
      }
    }

    public CallerChain JoinedChain {
      get {
        Current current = GetPICurrent();
        CallerChain chain;
        try {
          chain = current.get_slot(_joinedChainSlotId) as CallerChain;
        }
        catch (InvalidSlot e) {
          Logger.Fatal("Falha inesperada ao acessar o slot da joined chain.", e);
          throw;
        }
        return chain;
      }
    }

    public LoginRegistry LoginRegistry {
      get {
        ConnectionImpl conn = GetCurrentConnectionOrDefault() as ConnectionImpl;
        if (conn == null || !conn.Login.HasValue) {
          throw new NO_PERMISSION(NoLoginCode.ConstVal,
                                  CompletionStatus.Completed_No);
        }
        return conn.LoginRegistry;
      }
    }

    public OfferRegistry OfferRegistry {
      get {
        ConnectionImpl conn = GetCurrentConnectionOrDefault() as ConnectionImpl;
        if (conn == null || !conn.Login.HasValue) {
          throw new NO_PERMISSION(NoLoginCode.ConstVal,
                                  CompletionStatus.Completed_No);
        }
        return conn.Offers;
      }
    }

    #endregion

    #region Internal Members

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

    internal Connection GetCurrentConnectionOrDefault() {
      return GetCurrentConnection() ?? GetDefaultConnection();
    }

    private void SetConnectionById(Guid connectionId, Connection conn) {
      lock (_connections) {
        Connection removed;
        _connections.TryRemove(connectionId, out removed);
        if (conn != null) {
          _connections.TryAdd(connectionId, conn);
        }
      }
    }

    private Connection GetConnectionById(Guid connectionId) {
      Connection conn;
      return _connections.TryGetValue(connectionId, out conn) ? conn : null;
    }

    internal void IgnoreCurrentThread() {
      Current current = GetPICurrent();
      try {
        current.set_slot(_ignoreThreadSlotId, Boolean.TrueString);
      }
      catch (InvalidSlot e) {
        Logger.Fatal(
          "Falha inesperada ao acessar o slot de interceptação ignorada.", e);
        throw;
      }
    }

    internal void UnignoreCurrentThread() {
      Current current = GetPICurrent();
      try {
        current.set_slot(_ignoreThreadSlotId, Boolean.FalseString);
      }
      catch (InvalidSlot e) {
        Logger.Fatal(
          "Falha inesperada ao acessar o slot de interceptação ignorada.", e);
        throw;
      }
    }

    internal bool IsCurrentThreadIgnored(RequestInfo ri) {
      try {
        return Convert.ToBoolean(ri.get_slot(_ignoreThreadSlotId));
      }
      catch (InvalidSlot e) {
        Logger.Fatal(
          "Falha inesperada ao acessar o slot de interceptação ignorada.", e);
        throw;
      }
    }

    private void LogPropertyChanged(string prop, string value) {
      Logger.Info(String.Format("Propriedade {0} alterada para o valor {1}.",
                                prop, value));
    }

    internal int GetConnectionsMapSize() {
      return _connections.Count;
    }

    #endregion
  }
}