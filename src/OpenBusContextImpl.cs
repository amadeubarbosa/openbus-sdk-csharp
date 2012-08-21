using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using log4net;
using omg.org.CORBA;
using omg.org.PortableInterceptor;
using tecgraf.openbus.core.v1_05.access_control_service;
using tecgraf.openbus.core.v2_0.services.access_control;
using tecgraf.openbus.core.v2_0.services.offer_registry;
using tecgraf.openbus.exceptions;
using Current = omg.org.PortableInterceptor.Current;

//TODO fazer busca por variáveis com nome manager no sdk, interops e demos
//TODO revisar comentários de todas as outras interfaces
//TODO verificar nomes e comentários de testes
//TODO procurar pelas palavras requester e dispatcher no sdk, interops e demos
//TODO Tentar fazer com que createConnection não falhe nunca (não obtendo facetas, o que seria feito no login, e não fazendo narrow pro CORBA::Object do IComponent obtido pelo corbaloc. Pode ser que em C# não dê problema pois não tem narrow de CORBA, mas tem que testar usando valores válidos e inválidos.
//TODO no assistant testar construtores que recebem params (ver ideia com felipe) ou fabrica que vai dando set em tudo e depois quando pede create, reseta tudo que setou
//TODO ver todos os TODOs
namespace tecgraf.openbus {
  internal class OpenBusContextImpl : OpenBusContext {
    #region Fields

    private static readonly ILog Logger =
      LogManager.GetLogger(typeof (OpenBusContextImpl));

    /** Mapa de conexão por thread */
    //TODO tem que mudar de thread pra id gerado.
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

    private readonly int _joinedChainSlotId;

    private Connection _defaultConnection;
    private CallDispatchCallback _onCallDispatchCallback;
    //TODO rever se posso dividir em locks diferentes
    private readonly ReaderWriterLockSlim _lock;

    #endregion

    #region Constructors

    public OpenBusContextImpl(int currentThreadSlotId, int ignoreThreadSlotId, int joinedChainSlotId) {
      _connectedThreads = new ConcurrentDictionary<int, Connection>();
      CurrentThreadSlotId = currentThreadSlotId;
      _ignoreThreadSlotId = ignoreThreadSlotId;
      _joinedChainSlotId = joinedChainSlotId;
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

    public Connection GetCurrentConnection() {
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
               : GetDefaultConnection();
    }

    public Connection SetCurrentConnection(Connection conn) {
      Connection previous = GetCurrentConnection();
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
      SetConnectionByThreadId(id, conn);
      return previous;
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
        _loginLock.EnterReadLock();
        try {
          if (!_login.HasValue) {
            return null;
          }
        }
        finally {
          _loginLock.ExitReadLock();
        }
        if (!ReferenceEquals(piCurrentConn, this)) {
          return null;
        }
        try {
          AnyCredential anyCredential =
            (AnyCredential) current.get_slot(_credentialSlotId);
          if (anyCredential.IsLegacy) {
            Credential credential = anyCredential.LegacyCredential;
            LoginInfo caller = new LoginInfo(credential.identifier,
                                             credential.owner);
            LoginInfo[] originators = credential._delegate.Equals(String.Empty)
                                        ? new LoginInfo[0]
                                        : new[] {
                                                  new LoginInfo("<unknown>",
                                                                credential.
                                                                  _delegate)
                                                };
            return new CallerChainImpl(BusId, caller, originators);
          }
          CallChain chain = UnmarshalCallChain(anyCredential.Credential.chain);
          return new CallerChainImpl(BusId, chain.caller, chain.originators,
                                     anyCredential.Credential.chain);
        }
        catch (InvalidSlot e) {
          Logger.Fatal(
            "Falha inesperada ao acessar o slot da credencial corrente.", e);
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
        ConnectionImpl conn = GetCurrentConnection() as ConnectionImpl;
        if (conn == null) {
          throw new NO_PERMISSION(NoLoginCode.ConstVal, CompletionStatus.Completed_No);
        }
        return conn.LoginRegistry;
      }
    }

    public OfferRegistry OfferRegistry {
      get {
        ConnectionImpl conn = GetCurrentConnection() as ConnectionImpl;
        if (conn == null) {
          throw new NO_PERMISSION(NoLoginCode.ConstVal, CompletionStatus.Completed_No);
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

    #endregion
  }
}