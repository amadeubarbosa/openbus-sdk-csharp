using System;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Ch.Elca.Iiop;
using log4net;
using omg.org.CORBA;
using omg.org.IOP;
using omg.org.IOP.Codec_package;
using omg.org.PortableInterceptor;
using Org.BouncyCastle.Crypto;
using scs.core;
using tecgraf.openbus.core.v2_0.credential;
using tecgraf.openbus.core.v2_0.data_export;
using tecgraf.openbus.core.v2_1;
using tecgraf.openbus.core.v2_1.credential;
using tecgraf.openbus.core.v2_1.data_export;
using tecgraf.openbus.core.v2_1.services.access_control;
using tecgraf.openbus.core.v2_1.services.offer_registry;
using tecgraf.openbus.exceptions;
using tecgraf.openbus.interceptors;
using tecgraf.openbus.security;
using Current = omg.org.PortableInterceptor.Current;
using ExportedSharedAuth = tecgraf.openbus.core.v2_1.data_export.ExportedSharedAuth;
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
          (IComponent)OrbServices.CreateProxy(typeof(IComponent), corbaloc);
        return new ConnectionImpl(busIC, this, !GetLegacyDisableFromProps(props), GetPrivateKeyFromProps(props));
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
        return new ConnectionImpl(reference, this, !GetLegacyDisableFromProps(props), GetPrivateKeyFromProps(props));
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
        Logger.Error("Não há conexão para executar a chamada MakeChainFor.");
        throw new NO_PERMISSION(NoLoginCode.ConstVal, CompletionStatus.Completed_No);
      }
      LoginInfo? myLogin = conn.Login;
      if (!myLogin.HasValue) {
        Logger.Error("Não há login para executar a chamada MakeChainFor.");
        throw new NO_PERMISSION(NoLoginCode.ConstVal, CompletionStatus.Completed_No);
      }

      try {
        AccessControl acs = conn.Acs;
        SignedData signedChain = acs.signChainFor(entity);
        CallChain callChain = CallerChainImpl.UnmarshalCallChain(signedChain);
        if (conn.Legacy) {
          SignedCallChain legacySigned = conn.LegacyConverter.signChainFor(entity);
          return new CallerChainImpl(callChain.bus, callChain.caller, callChain.target,
            callChain.originators, signedChain, legacySigned);
        }
        return new CallerChainImpl(callChain.bus, callChain.caller, callChain.target,
          callChain.originators, signedChain);
      }
      catch (GenericUserException e) {
        Logger.Error("Falha inesperada ao criar uma nova cadeia.", e);
        throw;
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
        CallChain callChain = CallerChainImpl.UnmarshalCallChain(signedChain);
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
        VersionedData? actualVersion = null;
        VersionedData? legacyVersion = null;
        if (!chainImpl.Legacy) {
          // se não é legacy, tem a versão atual. Pode ter a versão legacy ou não.
          TypeCode signedChainTC = _orb.create_tc_for_type(typeof (SignedData));
          Any any = new Any(chainImpl.Signed.Chain, signedChainTC);
          byte[] encoded = _codec.encode_value(any);
          actualVersion = new VersionedData(ExportVersion.ConstVal, encoded);
          i++;
        }
        if (chainImpl.Signed.LegacyChain.encoded != null) {
          ExportedCallChain exported = new ExportedCallChain(chainImpl.BusId,
            chainImpl.Signed.LegacyChain);
          TypeCode exportedChainTC = _orb.create_tc_for_type(typeof(ExportedCallChain));
          Any any = new Any(exported, exportedChainTC);
          byte[] legacyEncoded = _codec.encode_value(any);
          legacyVersion = new VersionedData(CurrentVersion.ConstVal, legacyEncoded);
          i++;
        }
        VersionedData[] versions = new VersionedData[i];
        // A ordem das versões exportadas IMPORTA. A 2.1 deve vir antes da 2.0.
        if (legacyVersion != null) {
          versions[--i] = legacyVersion.Value;
        }
        if (actualVersion != null) {
          versions[--i] = actualVersion.Value;
        }
        return EncodeExportedVersions(versions, _magicTagCallChain);
      }
      catch (InvalidTypeForEncoding e) {
        const string message =
          "Falha inesperada ao codificar uma cadeia para exportação.";
        Logger.Error(message, e);
        throw new OpenBusInternalException(message, e);
      }
    }

    public CallerChain DecodeChain(byte[] encoded) {
      try {
        VersionedData[] versions = DecodeExportedVersions(encoded,
          _magicTagCallChain);
        CallerChainImpl decodedChain = null;
        for (int i = 0; i < versions.Length; i++) {
          // Se houver duas versões, a versão atual virá antes da versão legacy.
          if (versions[i].version == ExportVersion.ConstVal) {
            TypeCode signedDataTypeCode =
              ORB.create_tc_for_type(typeof(SignedData));
            SignedData exportedChain =
              (SignedData)
                _codec.decode_value(versions[i].encoded, signedDataTypeCode);
            CallChain chain = CallerChainImpl.UnmarshalCallChain(exportedChain);
            if (decodedChain == null) {
              decodedChain = new CallerChainImpl(chain.bus, chain.caller,
                chain.target, chain.originators, exportedChain);
            }
            else {
              decodedChain.Signed.Chain = exportedChain;
            }
          }
          if (versions[i].version == CurrentVersion.ConstVal) {
            TypeCode exportedChainTypeCode =
              ORB.create_tc_for_type(typeof(ExportedCallChain));
            ExportedCallChain exportedChain =
              (ExportedCallChain)
                _codec.decode_value(versions[i].encoded, exportedChainTypeCode);
            core.v2_0.services.access_control.CallChain chain =
              CallerChainImpl.UnmarshalLegacyCallChain(exportedChain.signedChain);
            if (decodedChain == null) {
              decodedChain = new CallerChainImpl(exportedChain.bus, chain.caller,
                chain.target, chain.originators, exportedChain.signedChain);
            }
            else {
              decodedChain.Signed.LegacyChain = exportedChain.signedChain;
            }
          }
        }
        if (decodedChain != null) {
          return decodedChain;
        }
      }
      catch (GenericUserException e) {
        const string message =
          "Falha inesperada ao decodificar uma cadeia exportada.";
        Logger.Error(message, e);
        throw new InvalidEncodedStreamException(message, e);
      }
      throw new InvalidEncodedStreamException("Versão de cadeia incompatível.");
    }

    public byte[] EncodeSharedAuth(SharedAuthSecret secret) {
      try {
        SharedAuthSecretImpl sharedAuth = (SharedAuthSecretImpl)secret;
        int i = 0;
        VersionedData? actualVersion = null;
        VersionedData? legacyVersion = null;
        if (!sharedAuth.Legacy) {
          // se não é legacy, tem a versão atual. Pode ter a versão legacy ou não.
          ExportedSharedAuth exportedAuth = new ExportedSharedAuth(sharedAuth.BusId, sharedAuth.Attempt, sharedAuth.Secret);
          TypeCode exportedAuthTC = _orb.create_tc_for_type(typeof(ExportedSharedAuth));
          Any any = new Any(exportedAuth, exportedAuthTC);
          byte[] encoded = _codec.encode_value(any);
          actualVersion = new VersionedData(ExportVersion.ConstVal, encoded);
          i++;
        }
        if (sharedAuth.LegacyAttempt != null) {
          core.v2_0.data_export.ExportedSharedAuth legacyAuth =
            new core.v2_0.data_export.ExportedSharedAuth(sharedAuth.BusId,
              sharedAuth.LegacyAttempt, sharedAuth.Secret);
          TypeCode legacyExportedAuthTC = _orb.create_tc_for_type(typeof(core.v2_0.data_export.ExportedSharedAuth));
          Any any = new Any(legacyAuth, legacyExportedAuthTC);
          byte[] legacyEncoded = _codec.encode_value(any);
          legacyVersion = new VersionedData(CurrentVersion.ConstVal, legacyEncoded);
          i++;
        }
        VersionedData[] versions = new VersionedData[i];
        // A ordem das versões exportadas IMPORTA. A 2.1 deve vir antes da 2.0.
        if (legacyVersion != null) {
          versions[--i] = legacyVersion.Value;
        }
        if (actualVersion != null) {
          versions[--i] = actualVersion.Value;
        }
        return EncodeExportedVersions(versions, _magicTagSharedAuth);
      }
      catch (InvalidTypeForEncoding e) {
        const string message =
          "Falha inesperada ao codificar uma autenticação compartilhada para exportação.";
        Logger.Error(message, e);
        throw new OpenBusInternalException(message, e);
      }
    }

    public SharedAuthSecret DecodeSharedAuth(byte[] encoded) {
      try {
        VersionedData[] versions = DecodeExportedVersions(encoded,
          _magicTagSharedAuth);
        SharedAuthSecretImpl sharedAuth = null;
        for (int i = 0; i < versions.Length; i++) {
          // Se houver duas versões, a versão atual virá antes da versão legacy.
          if (versions[i].version == ExportVersion.ConstVal) {
            TypeCode exportedAuthTypeCode =
              ORB.create_tc_for_type(typeof (ExportedSharedAuth));
            ExportedSharedAuth exportedAuth =
              (ExportedSharedAuth)
                _codec.decode_value(versions[i].encoded, exportedAuthTypeCode);
            if (sharedAuth == null) {
              sharedAuth = new SharedAuthSecretImpl(exportedAuth.bus, exportedAuth.attempt, exportedAuth.secret, null);
            }
            else {
              sharedAuth.Attempt = exportedAuth.attempt;
            }
          }
          if (versions[i].version == CurrentVersion.ConstVal) {
            TypeCode exportedAuthTypeCode =
              ORB.create_tc_for_type(typeof(core.v2_0.data_export.ExportedSharedAuth));
            core.v2_0.data_export.ExportedSharedAuth exportedAuth =
              (core.v2_0.data_export.ExportedSharedAuth)
                _codec.decode_value(versions[i].encoded, exportedAuthTypeCode);
            if (sharedAuth == null) {
              sharedAuth = new SharedAuthSecretImpl(exportedAuth.bus, null, exportedAuth.secret, exportedAuth.attempt);
            }
            else {
              sharedAuth.LegacyAttempt = exportedAuth.attempt;
            }
          }
        }
        if (sharedAuth != null) {
          return sharedAuth;
        }
      }
      catch (GenericUserException e) {
        const string message =
          "Falha inesperada ao decodificar uma autenticação compartilhada exportada.";
        Logger.Error(message, e);
        throw new InvalidEncodedStreamException(message, e);
      }
      throw new InvalidEncodedStreamException("Versão de autenticação compartilhada incompatível.");
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

    private byte[] EncodeExportedVersions(VersionedData[] exports, byte[] tag) {
      TypeCode versionedTypeCode = ORB.create_tc_for_type(typeof(VersionedData));
      TypeCode sequenceTypeCode = ORB.create_sequence_tc(0, versionedTypeCode);
      Any any = new Any(exports, sequenceTypeCode);
      byte[] encodedVersions = _codec.encode_value(any);
      byte[] fullEnconding = new byte[encodedVersions.Length + MagicTagSize];
      Buffer.BlockCopy(tag, 0, fullEnconding, 0, MagicTagSize);
      Buffer.BlockCopy(encodedVersions, 0, fullEnconding, MagicTagSize, encodedVersions.Length);
      return fullEnconding;
    }

    private VersionedData[] DecodeExportedVersions(byte[] encoded, byte[] tag) {
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
        TypeCode versionedTypeCode = ORB.create_tc_for_type(typeof(VersionedData));
        TypeCode sequenceTypeCode = ORB.create_sequence_tc(0, versionedTypeCode);
        return (VersionedData[])_codec.decode_value(encodedVersions, sequenceTypeCode);
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

    private bool GetLegacyDisableFromProps(ConnectionProperties props) {
      bool legacyDisable = ConnectionPropertiesImpl.LegacyDisableDefault;
      if (props != null) {
        if (props.LegacyDisable !=
            ConnectionPropertiesImpl.LegacyDisableDefault) {
          legacyDisable = props.LegacyDisable;
          LogPropertyChanged(ConnectionPropertiesImpl.LegacyDisableProperty,
                             legacyDisable.ToString(
                               CultureInfo.InvariantCulture));
        }
      }
      return legacyDisable;
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

    internal bool IsJoinedToLegacyChain() {
      CallerChainImpl joined = (CallerChainImpl)JoinedChain;
      return ((joined != null) && (joined.Legacy));
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
      lock (_connections) {
        Connection conn;
        return _connections.TryGetValue(connectionId, out conn) ? conn : null;
      }
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