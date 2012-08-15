﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using log4net;
using omg.org.CORBA;
using omg.org.PortableInterceptor;
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

    private const string LegacyDisableProperty = "legacy.disable";
    private const bool LegacyDisableDefault = false;
    private const string LegacyDelegateProperty = "legacy.delegate";
    private const string LegacyDelegateDefault = "caller";
    private const string LegacyDelegateOriginatorOption = "originator";

    // Identificador do slot de thread corrente.
    internal int CurrentThreadSlotId { get; private set; }

    // Identificador do slot de interceptação ignorada.
    private readonly int _ignoreThreadSlotId;

    #endregion

    #region ConnectionManager methods

    public ConnectionManagerImpl(int currentThreadSlotId, int ignoreThreadSlotId) {
      _connectedThreads = new ConcurrentDictionary<int, Connection>();
      _incomingDispatcherConn = new ConcurrentDictionary<string, Connection>();
      CurrentThreadSlotId = currentThreadSlotId;
      _ignoreThreadSlotId = ignoreThreadSlotId;
      _orb = OrbServices.GetSingleton();
    }

    public ORB ORB {
      get { return _orb; }
    }

    public Connection DefaultConnection { get; set; }

    public Connection CreateConnection(string host, ushort port,
                                       IDictionary<string, string> props) {
      IgnoreCurrentThread();
      try {
        bool legacyDisable = LegacyDisableDefault;
        if (props.ContainsKey(LegacyDisableProperty)) {
          string value = props[LegacyDisableProperty];
          if (!Boolean.TryParse(value, out legacyDisable)) {
            Logger.Error(
              String.Format("Valor {0} é inválido para a propriedade {1}.",
                            value, LegacyDisableProperty));
            throw new InvalidPropertyValueException(LegacyDisableProperty, value);
          }
          LogPropertyChanged(LegacyDisableProperty, legacyDisable.ToString());
        }
        bool originator = false;
        if (!legacyDisable) {
          if (props.ContainsKey(LegacyDelegateProperty)) {
            string value = props[LegacyDelegateProperty];
            string temp = value.ToLower();
            switch (temp) {
              case LegacyDelegateOriginatorOption:
                originator = true;
                break;
              case LegacyDelegateDefault:
                break;
              default:
                Logger.Error(
                  String.Format(
                    "Valor {0} é inválido para a propriedade {1}.",
                    value, LegacyDelegateProperty));
                throw new InvalidPropertyValueException(LegacyDelegateProperty,
                                                        value);
            }
            LogPropertyChanged(LegacyDelegateProperty, temp);
          }
        }
        return new ConnectionImpl(host, port, this, !legacyDisable, originator);
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
          Logger.Fatal("Falha inesperada ao acessar o slot da thread corrente",
                       e);
          throw;
        }

        Connection connection;
        return _connectedThreads.TryGetValue(id, out connection)
                 ? connection
                 : null;
      }
      set {
        int id = Thread.CurrentThread.ManagedThreadId;
        Current current = GetPICurrent();
        try {
          current.set_slot(CurrentThreadSlotId, id);
        }
        catch (InvalidSlot e) {
          Logger.Fatal("Falha inesperada ao acessar o slot da thread corrente",
                       e);
          throw;
        }
        SetConnectionByThreadId(id, value);
      }
    }

    public void SetDispatcher(Connection conn) {
      if (conn == null) {
        throw new ArgumentNullException();
      }
      lock (_incomingDispatcherConn) {
        Connection removed;
        _incomingDispatcherConn.TryRemove(conn.BusId, out removed);
        _incomingDispatcherConn.TryAdd(conn.BusId, conn);
      }
    }

    public Connection GetDispatcher(string busId) {
      Connection incoming;
      return _incomingDispatcherConn.TryGetValue(busId, out incoming)
               ? incoming
               : null;
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
      IList<Connection> list =
        new List<Connection>(_incomingDispatcherConn.Values);
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
      Logger.Info(String.Format("{0} property set to value {1}.", prop, value));
    }
  }
}