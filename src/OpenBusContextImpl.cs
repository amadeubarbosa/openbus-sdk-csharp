using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting;
using System.Threading;
using Ch.Elca.Iiop;
using Ch.Elca.Iiop.Idl;
using log4net;
using omg.org.CORBA;
using omg.org.IOP;
using omg.org.IOP.Codec_package;
using omg.org.PortableInterceptor;
using Org.BouncyCastle.Crypto;
using scs.core;
using tecgraf.openbus.core.v2_1;
using tecgraf.openbus.core.v2_1.credential;
using tecgraf.openbus.core.v2_1.data_export;
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

    private const int MagicTagSize = 4;
    private readonly byte[] _magicTagCallChain = { (byte)'B', (byte)'U', (byte)'S', 0x01 };
    private readonly byte[] _magicTagSharedAuth = { (byte)'B', (byte)'U', (byte)'S', 0x02 };

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

    public Connection ConnectByAddress(string host, ushort port,
      ConnectionProperties props) {
      if (host == null || host.Equals(string.Empty)) {
        throw new ArgumentException("Endereço inválido.");
      }
      if (port <= 0) {
        throw new ArgumentException("Porta inválida.");
      }
      IgnoreCurrentThread();
      try {
        String corbaloc = "corbaloc::1.0@" + host + ":" + port + "/" +
                    BusObjectKey.ConstVal;
        // RemotingServices.Connect não faz nenhuma chamada remota.
        IComponent busIC =
          (IComponent)RemotingServices.Connect(typeof(IComponent), corbaloc);
        return new ConnectionImpl(busIC, this, GetPrivateKeyFromProps(props));
      }
      finally {
        UnignoreCurrentThread();
      }
    }

    public Connection ConnectByReference(IComponent reference,
      ConnectionProperties props) {
      if (reference == null) {
        throw new ArgumentException("Referência para o barramento inválida.");
      }
      IgnoreCurrentThread();
      try {
        return new ConnectionImpl(reference, this, GetPrivateKeyFromProps(props));
      }
      finally {
        UnignoreCurrentThread();
      }
    }

    public Connection CreateConnection(string host, ushort port,
      ConnectionProperties props) {
      return ConnectByAddress(host, port, props);
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

    public CallerChain MakeChainFor(string entity) {
      ConnectionImpl conn = (ConnectionImpl) GetCurrentConnection();
      if (conn == null) {
        Logger.Error("Não há login para executar a chamada MakeChainFor.");
        throw new NO_PERMISSION(NoLoginCode.ConstVal, CompletionStatus.Completed_No);
      }
      AccessControl acs = conn.Acs;
      SignedData signedChain = acs.signChainFor(entity);
      try {
        CallChain callChain = UnmarshalCallChain(signedChain);
        return new CallerChainImpl(callChain.bus, callChain.caller, callChain.target,
          callChain.originators, signedChain);
      }
      catch (GenericUserException e) {
        const string message = "Falha inesperada ao criar uma nova cadeia.";
        Logger.Error(message, e);
        throw new OpenBusInternalException(message, e);
      }
    }

    public CallerChain ImportChain(byte[] token, string domain) {
      ConnectionImpl conn = (ConnectionImpl)GetCurrentConnection();
      if (conn == null) {
        throw new NO_PERMISSION(NoLoginCode.ConstVal, CompletionStatus.Completed_No);
      }
      AccessControl acs = conn.Acs;
      AsymmetricKeyParameter busKey = conn.BusKey;
      if (busKey == null) {
        throw new NO_PERMISSION(NoLoginCode.ConstVal, CompletionStatus.Completed_No);
      }
      byte[] encryptedToken = Crypto.Encrypt(busKey, token);
      SignedData signedChain = acs.signChainByToken(encryptedToken, domain);
      try {
        CallChain callChain = UnmarshalCallChain(signedChain);
        return new CallerChainImpl(callChain.bus, callChain.caller, callChain.target,
                                   callChain.originators, signedChain);
      }
      catch (GenericUserException e) {
        const string message = "Falha inesperada ao importar uma nova cadeia.";
        Logger.Error(message, e);
        throw new OpenBusInternalException(message, e);
      }
    }

    public byte[] EncodeChain(CallerChain chain) {
      try {
        CallerChainImpl chainImpl = (CallerChainImpl)chain;
        int i = 0;
        ExportedVersion[] versions;
        if (chain.IsLegacyChain()) {
          versions = new ExportedVersion[1];
        }
        else {
          // A ordem das versões exportadas IMPORTA. A 2.1 deve vir antes da 2.0.
          versions = new ExportedVersion[2];
          byte[] encoded = _codec.encode_value(chainImpl.Signed);
          versions[i] = new ExportedVersion(CurrentVersion.ConstVal, encoded);
          i++;
        }
        //TODO implementar. Incluir versão anterior da cadeia.
        throw new NotImplementedException();
        //return EncodeExportedVersions(versions, _magicTagCallChain);
      }
      catch (InvalidTypeForEncoding e) {
        const string message =
          "Falha inesperada ao codificar uma cadeia para exportação";
        Logger.Error(message, e);
        throw new OpenBusInternalException(message, e);
      }
    }

    public CallerChain DecodeChain(byte[] encoded) {
      try {
        ExportedVersion[] versions = DecodeExportedVersions(encoded,
          _magicTagCallChain);
        for (int i = 0; i < versions.Length; i++) {
          // Se houver duas versões, a versão atual virá antes da versão legacy.
          if (versions[i].version == CurrentVersion.ConstVal) {
            Type signedDataType = typeof(SignedData);
            TypeCode signedDataTypeCode =
              ORB.create_interface_tc(Repository.GetRepositoryID(signedDataType),
                signedDataType.Name);
            SignedData exportedChain =
              (SignedData)
                _codec.decode_value(versions[i].encoded, signedDataTypeCode);
            //TODO vale a pena verificar assinatura???
            CallChain chain = UnmarshalCallChain(exportedChain);
            return new CallerChainImpl(chain.bus, chain.caller,
              chain.target, chain.originators, exportedChain);
          }
          //TODO implementar. Decodificar cadeia legada.
          /*
          if (versions[i].version == LegacyVersion.ConstVal) {
            Type exportedCallChainType = typeof(LegacyExportedCallChain);
            TypeCode exportedCallChainTypeCode =
              ORB.create_interface_tc(Repository.GetRepositoryID(exportedCallChainType),
                exportedCallChainType.Name);
            LegacyExportedCallChain exportedChain =
              (LegacyExportedCallChain)
                _codec.decode_value(versions[i].encoded, exportedCallChainTypeCode);
            LoginInfo[] originators;
            if (!exportedChain._delegate.Equals("")) {
              originators = new LoginInfo[1];
              originators[0] = new LoginInfo(ConnectionImpl.LegacyOriginatorId, exportedChain._delegate);
            }
            else {
              originators = new LoginInfo[0];
            }
            CallChain chain = new CallChain(exportedChain.target, originators, exportedChain.caller);
            return new CallerChainImpl(exportedChain.bus, chain.caller,
              chain.target, chain.originators);
          }
          */
        }
        throw new InvalidEncodedStreamException("Versão de cadeia incompatível.");
      }
      catch (GenericUserException e) {
        const string message =
          "Falha inesperada ao decodificar uma cadeia exportada.";
        Logger.Error(message, e);
        throw new InvalidEncodedStreamException(message, e);
      }
    }

    public byte[] EncodeSharedAuth(SharedAuthSecret secret) {
      try {
        SharedAuthSecretImpl sharedAuth = (SharedAuthSecretImpl)secret;
        ExportedSharedAuth exportedAuth = new ExportedSharedAuth(sharedAuth.BusId, sharedAuth.Attempt, sharedAuth.Secret);
        byte[] encodedAuth = _codec.encode_value(exportedAuth);
        ExportedVersion[] versions = { new ExportedVersion(CurrentVersion.ConstVal, encodedAuth) };
        return EncodeExportedVersions(versions, _magicTagSharedAuth);
      }
      catch (InvalidTypeForEncoding e) {
        const string message =
          "Falha inesperada ao codificar um segredo de autenticação compartilhada para exportação";
        Logger.Error(message, e);
        throw new OpenBusInternalException(message, e);
      }
    }

    public SharedAuthSecret DecodeSharedAuth(byte[] encoded) {
      try {
        IEnumerable<ExportedVersion> versions = DecodeExportedVersions(encoded,
          _magicTagSharedAuth);
        foreach (ExportedVersion version in versions) {
          if (version.version == CurrentVersion.ConstVal) {
            Type exportedSharedAuthType = typeof(ExportedSharedAuth);
            TypeCode exportedSharedAuthTypeCode =
              ORB.create_interface_tc(Repository.GetRepositoryID(exportedSharedAuthType),
                exportedSharedAuthType.Name);
            ExportedSharedAuth secret =
              (ExportedSharedAuth)
                _codec.decode_value(version.encoded, exportedSharedAuthTypeCode);
            return new SharedAuthSecretImpl(secret.bus, secret.attempt, secret.secret);
          }
        }
        throw new InvalidEncodedStreamException("Versão de autenticação compartilhada incompatível.");
      }
      catch (GenericUserException e) {
        const string message =
          "Falha inesperada ao decodificar uma autenticação compartilhada exportada.";
        Logger.Error(message, e);
        throw new OpenBusInternalException(message, e);
      }
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

    private byte[] EncodeExportedVersions(ExportedVersion[] exports, byte[] tag) {
      byte[] encodedVersions = _codec.encode_value(exports);
      byte[] fullEnconding = new byte[encodedVersions.Length + MagicTagSize];
      Buffer.BlockCopy(tag, 0, fullEnconding, 0, MagicTagSize);
      Buffer.BlockCopy(encodedVersions, 0, fullEnconding, MagicTagSize, encodedVersions.Length);
      return fullEnconding;
    }

    private ExportedVersion[] DecodeExportedVersions(byte[] encoded, byte[] tag) {
      const string msg =
        "Stream de bytes não corresponde ao tipo de dado esperado.";
      if (encoded.Length <= ChainHeaderSize) {
        throw new InvalidEncodedStreamException(msg);
      }

      byte[] magicTag = new byte[MagicTagSize];
      byte[] encodedVersions = new byte[encoded.Length - MagicTagSize];
      Buffer.BlockCopy(encoded, 0, magicTag, 0, MagicTagSize);
      Buffer.BlockCopy(encoded, MagicTagSize, encodedVersions, 0,
                       encodedVersions.Length);
      if (tag.SequenceEqual(magicTag)) {
        Type exportedVersionType = typeof(ExportedVersion[]);
        TypeCode exportedVersionTypeCode =
          ORB.create_interface_tc(Repository.GetRepositoryID(exportedVersionType),
            exportedVersionType.Name);
        return (ExportedVersion[])_codec.decode_value(encodedVersions, exportedVersionTypeCode);
      }
      throw new InvalidEncodedStreamException(msg);
    }

    private PrivateKeyImpl GetPrivateKeyFromProps(ConnectionProperties props) {
      PrivateKeyImpl accessKey = null;
      if (props != null) {
        if (props.AccessKey != null) {
          accessKey = (PrivateKeyImpl)props.AccessKey;
          LogPropertyChanged(ConnectionPropertiesImpl.AccessKeyProperty,
            "{AccessKey provida pelo usuário}");
        }
      }
      return accessKey;
    }

    internal CallChain UnmarshalCallChain(SignedData signed) {
      Type chainType = typeof(CallChain);
      TypeCode chainTypeCode =
        ORB.create_interface_tc(Repository.GetRepositoryID(chainType),
                                chainType.Name);
      return (CallChain)_codec.decode_value(signed.encoded, chainTypeCode);
    }

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