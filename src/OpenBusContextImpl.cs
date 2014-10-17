using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Ch.Elca.Iiop;
using Ch.Elca.Iiop.Idl;
using log4net;
using omg.org.CORBA;
using omg.org.IOP;
using omg.org.IOP.Codec_package;
using omg.org.PortableInterceptor;
using tecgraf.openbus.core.v2_1.credential;
using tecgraf.openbus.core.v2_1.services.access_control;
using tecgraf.openbus.core.v2_1.services.offer_registry;
using tecgraf.openbus.exceptions;
using tecgraf.openbus.interceptors;
using tecgraf.openbus.security;
using Current = omg.org.PortableInterceptor.Current;
using TypeCode = omg.org.CORBA.TypeCode;

namespace tecgraf.openbus {
  internal class OpenBusContextImpl : OpenBusContext {
    #region Fields

    private static readonly ILog Logger =
      LogManager.GetLogger(typeof (OpenBusContextImpl));

    // Constante que define o tamanho em bytes da codificação do identificador da versão da cadeia de chamadas exportada.
    private const int ChainHeaderSize = 8;

    private readonly Codec _codec;

    // Mapa de conexões thread-safe. Associa uma referência fraca a uma conexão. Como as comparações de chave são feitas baseadas no endereço do objeto, o próprio endereço já serve como identificador.
    // Remove automaticamente entradas não mais utilizadas para evitar memory leak das threads de recebimento de chamadas.
    private readonly ConditionalWeakTable<Object, Connection> _connections;

    private readonly OrbServices _orb;

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
      _connections = new ConditionalWeakTable<Object, Connection>();
      _connectionIdSlotId = connectionIdSlotId;
      _ignoreThreadSlotId = ignoreThreadSlotId;
      _joinedChainSlotId = joinedChainSlotId;
      _chainSlotId = chainSlotId;
      _orb = OrbServices.GetSingleton();
      _lock = new ReaderWriterLockSlim();
      _codec = InterceptorsInitializer.Codec;
    }

    #endregion

    #region OpenBusContext methods

    public OrbServices ORB {
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
        PrivateKeyImpl accessKey = null;
        if (props != null) {
          if (props.AccessKey != null) {
            accessKey = (PrivateKeyImpl) props.AccessKey;
            LogPropertyChanged(ConnectionPropertiesImpl.AccessKeyProperty,
              "{AccessKey provida pelo usuário}");
          }
        }
        return new ConnectionImpl(host, port, this, accessKey);
      }
      finally {
        UnignoreCurrentThread();
      }
    }

    public Connection GetCurrentConnection() {
      try {
        Object guid = GetPICurrent().get_slot(_connectionIdSlotId);
        Connection current = guid != null ? GetConnectionById(guid) : null;
        return current ?? GetDefaultConnection();
      }
      catch (InvalidSlot e) {
        Logger.Fatal(ConnectionIdErrorMsg, e);
        throw;
      }
    }

    public Connection SetCurrentConnection(Connection conn) {
      // somente ignora o guid
      Object guid;
      return SetCurrentConnection(conn, out guid);
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

    public CallerChain MakeChainFor(string loginId) {
      ConnectionImpl conn = (ConnectionImpl) GetCurrentConnection();
      String busid = conn.BusId;
      SignedData signedChain = conn.Acs.signChainFor(loginId);
      try {
        CallChain callChain = UnmarshalCallChain(signedChain);
        return new CallerChainImpl(busid, callChain.caller, callChain.target,
          callChain.originators, signedChain);
      }
      catch (GenericUserException e) {
        const string message = "Falha inesperada ao criar uma nova cadeia.";
        Logger.Error(message, e);
        throw new OpenBusInternalException(message, e);
      }
    }

    public byte[] EncodeChain(CallerChain chain) {
      CallerChainImpl chainImpl = (CallerChainImpl) chain;
      ExportedCallChain exportedChain = new ExportedCallChain(chain.BusId,
        chainImpl.Signed);
      const int id = CredentialContextId.ConstVal;
      byte[] encodedChain;
      byte[] encodedId;
      try {
        encodedChain = _codec.encode_value(exportedChain);
        encodedId = _codec.encode_value(id);
      }
      catch (InvalidTypeForEncoding e) {
        const string message =
          "Falha inesperada ao codificar uma cadeia para exportação";
        Logger.Error(message, e);
        throw new OpenBusInternalException(message, e);
      }
      byte[] fullEnconding = new byte[encodedChain.Length + encodedId.Length];
      Buffer.BlockCopy(encodedId, 0, fullEnconding, 0, encodedId.Length);
      Buffer.BlockCopy(encodedChain, 0, fullEnconding, encodedId.Length,
        encodedChain.Length);
      return fullEnconding;
    }

    public CallerChain DecodeChain(byte[] encoded) {
      if (encoded.Length <= ChainHeaderSize) {
        const string msg =
          "Stream de bytes não corresponde a uma cadeia de chamadas.";
        throw new InvalidChainStreamException(msg);
      }
      ExportedCallChain importedChain;
      CallChain callChain;

      byte[] encodedId = new byte[ChainHeaderSize];
      byte[] encodedChain = new byte[encoded.Length - ChainHeaderSize];
      Buffer.BlockCopy(encoded, 0, encodedId, 0, encodedId.Length);
      Buffer.BlockCopy(encoded, encodedId.Length, encodedChain, 0,
        encodedChain.Length);
      try {
        int id = (int) _codec.decode_value(encodedId, _orb.create_long_tc());
        if (CredentialContextId.ConstVal != id) {
          String msg =
            String.Format(
              "Formato da cadeia é de versão incompatível.\nFormato recebido = {0}\nFormato suportado = {1}",
              id, CredentialContextId.ConstVal);
          throw new InvalidChainStreamException(msg);
        }
        Type exportedChainType = typeof (ExportedCallChain);
        TypeCode exportedChainTypeCode =
          ORB.create_interface_tc(Repository.GetRepositoryID(exportedChainType),
            exportedChainType.Name);
        importedChain =
          (ExportedCallChain)
            _codec.decode_value(encodedChain, exportedChainTypeCode);
        callChain = UnmarshalCallChain(importedChain.signedChain);
      }
      catch (GenericUserException e) {
        const string message =
          "Falha inesperada ao decodificar uma cadeia exportada.";
        Logger.Error(message, e);
        throw new OpenBusInternalException(message, e);
      }
      return new CallerChainImpl(importedChain.bus, callChain.caller,
        callChain.target, callChain.originators,
        importedChain.signedChain);
    }

    internal CallChain UnmarshalCallChain(SignedData signed) {
      Type chainType = typeof (CallChain);
      TypeCode chainTypeCode =
        ORB.create_interface_tc(Repository.GetRepositoryID(chainType),
          chainType.Name);
      return (CallChain) _codec.decode_value(signed.encoded, chainTypeCode);
    }

    public LoginRegistry LoginRegistry {
      get {
        ConnectionImpl conn = GetCurrentConnection() as ConnectionImpl;
        if (conn == null || !conn.Login.HasValue) {
          throw new NO_PERMISSION(NoLoginCode.ConstVal,
            CompletionStatus.Completed_No);
        }
        return conn.LoginRegistry;
      }
    }

    public OfferRegistry OfferRegistry {
      get {
        ConnectionImpl conn = GetCurrentConnection() as ConnectionImpl;
        if (conn == null || !conn.Login.HasValue) {
          throw new NO_PERMISSION(NoLoginCode.ConstVal,
            CompletionStatus.Completed_No);
        }
        return conn.Offers;
      }
    }

    #endregion

    #region Internal Members

    internal void SetCurrentConnection(Connection conn, ServerRequestInfo ri) {
      Object guid;
      SetCurrentConnection(conn, out guid);
      if (conn != null && guid != null) {
        ri.set_slot(_connectionIdSlotId, guid);
      }
    }

    private Connection SetCurrentConnection(Connection conn, out Object guid) {
      Connection previous = null;
      Current current = GetPICurrent();
      try {
        // tenta reaproveitar o guid
        guid = current.get_slot(_connectionIdSlotId);
        if (guid != null) {
          previous = GetConnectionById(guid);
          if (conn == null) {
            current.set_slot(_connectionIdSlotId, null);
            SetConnectionById(guid, null);
            return previous;
          }
        }
        else {
          if (conn == null) {
            return null;
          }
          guid = new Object();
          current.set_slot(_connectionIdSlotId, guid);
        }
        SetConnectionById(guid, conn);
        return previous;
      }
      catch (InvalidSlot e) {
        Logger.Fatal(ConnectionIdErrorMsg, e);
        throw;
      }
    }

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

    private void SetConnectionById(Object connectionId, Connection conn) {
      lock (_connections) {
        _connections.Remove(connectionId);
        if (conn != null) {
          _connections.Add(connectionId, conn);
        }
      }
    }

    private Connection GetConnectionById(Object connectionId) {
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

    #endregion
  }
}