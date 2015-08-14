using System;
using System.Collections;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Threading;
using Ch.Elca.Iiop;
using Ch.Elca.Iiop.Idl;
using Ch.Elca.Iiop.Security.Ssl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using omg.org.CORBA;
using scs.core;
using Scs.Core;
using tecgraf.openbus.core.v2_1.credential;
using tecgraf.openbus.core.v2_1.services.access_control;
using tecgraf.openbus.core.v2_1.services.offer_registry;
using tecgraf.openbus.exceptions;
using tecgraf.openbus.interop.simple;
using tecgraf.openbus.security;
using tecgraf.openbus.test;
using test;

namespace tecgraf.openbus.Test {
  /// <summary>
  ///   This is a test class for OpenBusContextTest and is intended
  ///   to contain all OpenBusContext Unit Tests
  /// </summary>
  [TestClass]
  public class OpenBusContextTest {
    #region Fields

    private static string _hostName;
    private static ushort _hostPort;
    private static string _hostName2;
    private static ushort _hostPort2;
    private static string _busIOR;
    private static string _busIOR2;
    private static MarshalByRefObject _busRef;
    private static MarshalByRefObject _busRef2;
    private static string _entity;
    private static string _login;
    private static byte[] _password;
    private static string _domain;
    private static PrivateKey _accessKey;
    private static bool _useSSL;
    private static OpenBusContext _context;
    private const string FakeEntity = "Fake Entity";
    private const string Unknown = "<unknown>";
    private const int Timeout = 10;
    private static Exception _threadRet;
    private static readonly Object ThreadRetLock = new object();

    private static readonly ConnectionProperties Props =
      new ConnectionPropertiesImpl();

    /// <summary>
    ///   Gets or sets the test _context which provides
    ///   information about and functionality for the current test run.
    /// </summary>
    public TestContext TestContext { get; set; }

    #endregion

    #region Additional test attributes

    // 
    //You can use the following additional attributes as you write your tests:
    //
    // Use ClassCleanup to run code after all tests in a class have run
    //[ClassCleanup]
    //public static void MyClassCleanup() {
    //}
    //
    // Use TestCleanup to run code after each test has run
    //[TestCleanup]
    //public void MyTestCleanup() {
    //}
    //

    #endregion

    //Use ClassInitialize to run code before running the first test in the class
    [ClassInitialize]
    public static void MyClassInitialize(TestContext testContext) {
      _hostName = ConfigurationManager.AppSettings["hostName"];

      string port = ConfigurationManager.AppSettings["hostPort"];
      if (!String.IsNullOrEmpty(port)) {
        _hostPort = ushort.Parse(port);
      }

      _hostName2 = ConfigurationManager.AppSettings["hostName2"];

      string port2 = ConfigurationManager.AppSettings["hostPort2"];
      if (!String.IsNullOrEmpty(port2)) {
        _hostPort2 = ushort.Parse(port2);
      }

      _entity = ConfigurationManager.AppSettings["entityName"];
      if (String.IsNullOrEmpty(_entity)) {
        throw new ArgumentNullException("entityName");
      }

      _login = ConfigurationManager.AppSettings["userLogin"];
      if (String.IsNullOrEmpty(_login)) {
        throw new ArgumentNullException("userLogin");
      }

      string password = ConfigurationManager.AppSettings["userPassword"];
      if (password == null) {
        throw new ArgumentNullException("userPassword");
      }
      _password = password.Equals("") ? new byte[0] : Crypto.TextEncoding.GetBytes(password);

      _domain = ConfigurationManager.AppSettings["userDomain"];
      if (String.IsNullOrEmpty(_domain)) {
        throw new ArgumentNullException("userDomain");
      }

      string privateKey = ConfigurationManager.AppSettings["testKeyFileName"];
      if (String.IsNullOrEmpty(password)) {
        throw new ArgumentNullException("testKeyFileName");
      }
      _accessKey = Crypto.ReadKeyFile(privateKey);
      Props.AccessKey = _accessKey;

      string useSSL = ConfigurationManager.AppSettings["useSSL"];
      if (String.IsNullOrEmpty(useSSL)) {
        throw new ArgumentNullException("useSSL");
      }
      _useSSL = Boolean.Parse(useSSL);

      if ((!_useSSL) && (String.IsNullOrEmpty(_hostName))) {
        throw new ArgumentNullException("hostName");
      }

      if (_useSSL) {
        IDictionary props = new Hashtable();
        props[IiopChannel.CHANNEL_NAME_KEY] = "SecuredServerIiopChannel";
        props[IiopChannel.TRANSPORT_FACTORY_KEY] =
            "Ch.Elca.Iiop.Security.Ssl.SslTransportFactory,SSLPlugin";

        props[Authentication.CheckCertificateRevocation] = false;

        props[SSLClient.ClientEncryptionType] = Encryption.EncryptionType.Required;
        props[SSLClient.ClientAuthentication] = SSLClient.ClientAuthenticationType.Supported;
        props[SSLClient.ServerAuthentication] = SSLClient.ServerAuthenticationType.Required;
        props[SSLClient.ClientAuthenticationClass] = typeof(ClientAuthenticationSpecificFromStore);
        props[SSLClient.CheckServerName] = false;
        // take certificates from the windows certificate store of the current user
        props[ClientAuthenticationSpecificFromStore.StoreLocation] =
          "CurrentUser";
        props[ClientAuthenticationSpecificFromStore.ClientCertificate] =
          "f838ccf3cdfa001ed860f94248dc8d603d06935f";

        props[IiopServerChannel.PORT_KEY] = "58000";
        props[SSLServer.SecurePort] = "58001";
        props[SSLServer.ServerEncryptionType] = Encryption.EncryptionType.Required;
        props[SSLServer.ClientAuthentication] = SSLServer.ClientAuthenticationType.Required;
        props[SSLServer.ServerAuthentication] = SSLServer.ServerAuthenticationType.Supported;
        props[SSLServer.ServerAuthenticationClass] = typeof(DefaultServerAuthenticationImpl);
        props[DefaultServerAuthenticationImpl.ServerCertificate] =
            "f838ccf3cdfa001ed860f94248dc8d603d06935f";
        props[DefaultServerAuthenticationImpl.StoreLocation] = "CurrentUser";

        ORBInitializer.InitORB(props);
        _busIOR = ConfigurationManager.AppSettings["busIOR"];
        if (String.IsNullOrEmpty(_busIOR)) {
          throw new InvalidPropertyValueException(_busIOR);
        }
        _busIOR2 = ConfigurationManager.AppSettings["busIOR2"];
        if (String.IsNullOrEmpty(_busIOR2)) {
          throw new InvalidPropertyValueException(_busIOR2);
        }
        string[] iors = File.ReadAllLines(_busIOR);
        _busIOR = iors[0];
        _busRef =
          (MarshalByRefObject)OrbServices.CreateProxy(typeof(IComponent), _busIOR);
        string[] iors2 = File.ReadAllLines(_busIOR2);
        _busIOR2 = iors2[0];
        _busRef2 =
          (MarshalByRefObject)OrbServices.CreateProxy(typeof(IComponent), _busIOR2);
      }
      else {
        ORBInitializer.InitORB();
      }
      _context = ORBInitializer.Context;
    }

    // Use TestInitialize to run code before running each test
    [TestInitialize]
    public void MyTestInitialize() {
      _context.ExitChain();
      _context.SetCurrentConnection(null);
      _context.SetDefaultConnection(null);
      _context.OnCallDispatch = null;
      lock (ThreadRetLock) {
        _threadRet = null;
      }
    }

    /// <summary>
    ///   Teste da auto-propriedade ORB
    /// </summary>
    [TestMethod]
    public void ORBTest() {
      Assert.IsNotNull(ORBInitializer.Context.ORB);
    }

    /// <summary>
    ///   Testes do método CreateConnection
    /// </summary>
    [TestMethod]
    public void CreateConnectionTest() {
      // cria conexão válida
      Assert.IsNotNull(ConnectToBus());
      // tenta criar conexão com hosts inválidos
      Connection invalid = null;
      if (_useSSL) {
        try {
          invalid = ConnectToBus(null, 0, null, Props);
        }
        catch (ArgumentException) {
        }
        finally {
          Assert.IsNull(invalid);
        }
      }
      else {
        try {
          invalid = ConnectToBus(null, _hostPort, null, Props);
        }
        catch (ArgumentException) {
        }
        finally {
          Assert.IsNull(invalid);
        }
        try {
          invalid = ConnectToBus("", _hostPort, null, Props);
        }
        catch (ArgumentException) {
        }
        finally {
          Assert.IsNull(invalid);
        }
        try {
          invalid = ConnectToBus(_hostName, 0, null, Props);
        }
        catch (ArgumentException) {
        }
        finally {
          Assert.IsNull(invalid);
        }
      }
    }

    /// <summary>
    ///   Testes da auto-propriedade OnCallDispatch
    /// </summary>
    [TestMethod]
    public void OnCallDispatchCallbackTest() {
      Connection conn = ConnectToBus();
      Assert.IsNull(_context.OnCallDispatch);
      CallDispatchCallbackImpl callback = new CallDispatchCallbackImpl(conn);
      _context.OnCallDispatch = callback.Dispatch;
      Assert.AreEqual(callback.Dispatch, _context.OnCallDispatch);
      _context.OnCallDispatch = null;
      Assert.IsNull(_context.OnCallDispatch);
    }

    /// <summary>
    ///   Teste do DefaultConnection
    /// </summary>
    [TestMethod]
    public void DefaultConnectionTest() {
      _context.SetDefaultConnection(null);
      Connection conn = ConnectToBus();
      conn.LoginByPassword(_login, _password, _domain);
      Assert.IsNull(_context.GetDefaultConnection());
      _context.SetCurrentConnection(conn);
      Assert.IsNull(_context.GetDefaultConnection());
      _context.SetCurrentConnection(null);
      Connection previous = _context.SetDefaultConnection(conn);
      Assert.IsNull(previous);
      Assert.AreEqual(_context.GetDefaultConnection(), conn);
      CallDispatchCallbackImpl callback = new CallDispatchCallbackImpl(conn);
      _context.OnCallDispatch = callback.Dispatch;
      Assert.AreEqual(_context.GetDefaultConnection(), conn);
      _context.OnCallDispatch = null;
      Assert.IsTrue(conn.Logout());
      Assert.AreEqual(_context.GetDefaultConnection(), conn);
      previous = _context.SetDefaultConnection(null);
      Assert.AreEqual(previous, conn);

      // tentativa de chamada sem current connection setado nem default connection
      conn.LoginByPassword(_login, _password, _domain);
      Assert.IsNull(_context.GetDefaultConnection());
      Assert.IsNull(_context.GetCurrentConnection());
      bool failed = false;
      ServiceProperty[] props = {new ServiceProperty("a", "b")};
      try {
        _context.OfferRegistry.findServices(props);
      }
      catch (NO_PERMISSION e) {
        failed = true;
        if (e.Minor != NoLoginCode.ConstVal) {
          Assert.Fail(
            "A exceção deveria ser NO_PERMISSION com minor code NoLoginCode. Minor code recebido: " +
            e.Minor);
        }
      }
      catch (Exception e) {
        Assert.Fail(
          "A exceção deveria ser NO_PERMISSION com minor code NoLoginCode. Exceção recebida: " +
          e);
      }
      Assert.IsTrue(failed);
      // tentativa com default connection setado
      previous = _context.SetDefaultConnection(conn);
      Assert.IsNull(previous);
      try {
        _context.OfferRegistry.findServices(props);
      }
      catch (Exception e) {
        Assert.Fail(
          "A chamada com default connection setado deveria ser bem-sucedida. Exceção recebida: " +
          e);
      }
      finally {
        previous = _context.SetDefaultConnection(null);
      }
      Assert.AreEqual(previous, conn);
    }

    /// <summary>
    ///   Teste do CurrentConnection
    /// </summary>
    [TestMethod]
    public void CurrentConnectionTest() {
      Connection conn = ConnectToBus();
      conn.LoginByPassword(_login, _password, _domain);
      Assert.IsNull(_context.GetCurrentConnection());
      _context.SetDefaultConnection(conn);
      CallDispatchCallbackImpl callback = new CallDispatchCallbackImpl(conn);
      _context.OnCallDispatch = callback.Dispatch;
      Assert.AreEqual(_context.GetCurrentConnection(), conn);
      _context.SetCurrentConnection(conn);
      Assert.AreEqual(_context.GetCurrentConnection(), conn);
      _context.SetDefaultConnection(null);
      _context.OnCallDispatch = null;
      Assert.IsTrue(conn.Logout());
      Assert.AreEqual(_context.GetCurrentConnection(), conn);
      Connection previous = _context.SetCurrentConnection(null);
      Assert.AreEqual(previous, conn);

      // tentativa de chamada sem current connection setado nem default connection
      conn.LoginByPassword(_login, _password, _domain);
      Assert.IsNull(_context.GetCurrentConnection());
      bool failed = false;
      ServiceProperty[] props = {new ServiceProperty("a", "b")};
      try {
        _context.OfferRegistry.findServices(props);
      }
      catch (NO_PERMISSION e) {
        failed = true;
        if (e.Minor != NoLoginCode.ConstVal) {
          Assert.Fail(
            "A exceção deveria ser NO_PERMISSION com minor code NoLoginCode. Minor code recebido: " +
            e.Minor);
        }
      }
      catch (Exception e) {
        Assert.Fail(
          "A exceção deveria ser NO_PERMISSION com minor code NoLoginCode. Exceção recebida: " +
          e);
      }
      Assert.IsTrue(failed);
      // tentativa com current connection setado
      previous = _context.SetCurrentConnection(conn);
      Assert.IsNull(previous);
      try {
        _context.OfferRegistry.findServices(props);
      }
      catch (Exception e) {
        Assert.Fail(
          "A chamada com current connection setado deveria ser bem-sucedida. Exceção recebida: " +
          e);
      }
      finally {
        previous = _context.SetCurrentConnection(null);
      }
      Assert.AreEqual(previous, conn);
    }

    /// <summary>
    ///   Testes da auto-propriedade CallerChain
    /// </summary>
    [TestMethod]
    public void CallerChainTest() {
      Connection conn = ConnectToBus();
      _context.SetDefaultConnection(conn);
      Assert.IsNull(_context.CallerChain);
      try {
        const string facetName = "HelloMock";
        conn.LoginByPassword(_login, _password, _domain);
        ComponentContext component =
          new DefaultComponentContext(new ComponentId());
        component.AddFacet(facetName,
          Repository.GetRepositoryID(typeof (Hello)),
          new HelloMock());
        //TODO: o campo Reference abaixo é o objeto servant e não um stub devido a um bug do SCS. Após ser consertado, pode-se remover a transformação em IOR.
        Hello hello = component.GetFacetByName(facetName).Reference as Hello;
        string temp = _context.ORB.object_to_string(hello);
        Hello helloStub = (Hello) _context.ORB.string_to_object(temp);
        Assert.IsNotNull(helloStub);
        helloStub.sayHello();
      }
      catch (UNKNOWN) {
        Assert.Fail("A cadeia obtida é nula ou não é a esperada.");
      }
      finally {
        _context.SetDefaultConnection(null);
        conn.Logout();
      }
    }

    /// <summary>
    ///   Testes do método JoinChain
    /// </summary>
    [TestMethod]
    public void JoinChainTest() {
      Connection conn = ConnectToBus();
      _context.SetCurrentConnection(conn);
      try {
        Assert.IsNull(_context.JoinedChain);
        // adiciona a chain da getCallerChain
        _context.JoinChain(null);
        Assert.IsNull(_context.JoinedChain);
        //TODO testar caso em que a chain da getCallerChain não é vazia
        //TODO não há como testar o caso do TODO acima em C# sem usar processos diferentes para o servidor e cliente. Não há muito problema pois os testes de interoperabilidade cobrem esse caso.
        conn.LoginByPassword(_login, _password, _domain);
        Assert.IsNotNull(conn.Login);
        _context.JoinChain(new CallerChainImpl("mock", new LoginInfo("a", "b"),
          conn.Login.Value.entity, new LoginInfo[0],
          ConnectionImpl.InvalidSignedData));
        Assert.IsNotNull(_context.JoinedChain);
        Assert.AreEqual("mock", _context.JoinedChain.BusId);
        Assert.AreEqual("a", _context.JoinedChain.Caller.id);
        Assert.AreEqual("b", _context.JoinedChain.Caller.entity);
        Assert.AreEqual(ConnectionImpl.InvalidSignedData,
          ((CallerChainImpl) _context.JoinedChain).Signed.Chain);
        _context.ExitChain();
        Assert.IsNull(_context.JoinedChain);
        Assert.IsTrue(conn.Logout());
      }
      finally {
        _context.SetCurrentConnection(null);
      }
    }

    /// <summary>
    ///   Testes do método ExitChain
    /// </summary>
    [TestMethod]
    public void ExitChainTest() {
      Connection conn = ConnectToBus();
      _context.SetCurrentConnection(conn);
      try {
        Assert.IsNull(_context.JoinedChain);
        _context.ExitChain();
        Assert.IsNull(_context.JoinedChain);
        conn.LoginByPassword(_login, _password, _domain);
        Assert.IsNotNull(conn.Login);
        _context.JoinChain(new CallerChainImpl("mock", new LoginInfo("a", "b"),
          conn.Login.Value.entity, new LoginInfo[0],
          ConnectionImpl.InvalidSignedData));
        Assert.IsNotNull(_context.JoinedChain);
        _context.ExitChain();
        Assert.IsNull(_context.JoinedChain);
        Assert.IsTrue(conn.Logout());
      }
      finally {
        _context.SetCurrentConnection(null);
      }
    }

    /// <summary>
    ///   Testes do método MakeChainFor
    /// </summary>
    [TestMethod]
    public void MakeChainForTest() {
      Connection conn1 = ConnectToBus();
      const string actor1 = "actor-1";
      conn1.LoginByPassword(actor1, Crypto.TextEncoding.GetBytes(actor1),
        _domain);
      Connection conn2 = ConnectToBus();
      const string actor2 = "actor-2";
      conn2.LoginByPassword(actor2, Crypto.TextEncoding.GetBytes(actor2),
        _domain);

      _context.SetCurrentConnection(conn1);
      try {
        CallerChain chain1To2 = _context.MakeChainFor(conn2.Login.Value.entity);
        Assert.AreEqual(actor2, chain1To2.Target);
        Assert.AreEqual(actor1, chain1To2.Caller.entity);
        Assert.AreEqual(conn1.Login.Value.id, chain1To2.Caller.id);
      }
      finally {
        _context.SetCurrentConnection(null);
      }
      conn1.Logout();
      conn2.Logout();
    }

    /// <summary>
    ///   Testes com join do método MakeChainFor
    /// </summary>
    [TestMethod]
    public void MakeChainForJoinedTest() {
      Connection conn1 = ConnectToBus();
      const string actor1 = "actor-1";
      conn1.LoginByPassword(actor1, Crypto.TextEncoding.GetBytes(actor1),
        _domain);
      Connection conn2 = ConnectToBus();
      const string actor2 = "actor-2";
      conn2.LoginByPassword(actor2, Crypto.TextEncoding.GetBytes(actor2),
        _domain);
      Connection conn3 = ConnectToBus();
      const string actor3 = "actor-3";
      conn3.LoginByPassword(actor3, Crypto.TextEncoding.GetBytes(actor3),
        _domain);

      _context.SetCurrentConnection(conn1);
      try {
        CallerChain chain1To2 = _context.MakeChainFor(conn2.Login.Value.entity);
        Assert.AreEqual(actor2, chain1To2.Target);
        Assert.AreEqual(actor1, chain1To2.Caller.entity);
        Assert.AreEqual(conn1.Login.Value.id, chain1To2.Caller.id);

        _context.SetCurrentConnection(conn2);
        _context.JoinChain(chain1To2);
        CallerChain chain1_2To3 = _context.MakeChainFor(conn3.Login.Value.entity);
        Assert.AreEqual(actor3, chain1_2To3.Target);
        Assert.AreEqual(actor2, chain1_2To3.Caller.entity);
        Assert.AreEqual(conn2.Login.Value.id, chain1_2To3.Caller.id);
        LoginInfo[] originators = chain1_2To3.Originators;
        Assert.IsTrue(originators.Length > 0);
        LoginInfo info1 = originators[0];
        Assert.AreEqual(actor1, info1.entity);
        Assert.AreEqual(conn1.Login.Value.id, info1.id);
        _context.ExitChain();
      }
      finally {
        _context.SetCurrentConnection(null);
      }
      conn1.Logout();
      conn2.Logout();
      conn3.Logout();
    }

    /// <summary>
    ///   Testes com join em cadeia atual com originator usando cadeia legada do método MakeChainFor
    /// </summary>
    [TestMethod]
    public void MakeChainForJoinedInCurrentChainWithLegacyOriginatorTest() {
      Connection conn1 = ConnectToBus();
      const string actor1 = "actor-1";
      conn1.LoginByPassword(actor1, Crypto.TextEncoding.GetBytes(actor1),
        _domain);
      Connection conn2 = ConnectToBus();
      conn2.LoginByCertificate(_entity, _accessKey);
      Connection conn3 = ConnectToBus();
      const string actor3 = "actor-3";
      conn3.LoginByPassword(actor3, Crypto.TextEncoding.GetBytes(actor3),
        _domain);

      _context.SetCurrentConnection(conn1);
      try {
        // cadeia originator legada
        CallerChainImpl legacyChain =
          (CallerChainImpl) _context.MakeChainFor(_entity);
        legacyChain.Legacy = true;
        legacyChain.Signed.Chain = new SignedData();
        // cadeia do meio sem alterações
        _context.SetCurrentConnection(conn2);
        _context.JoinChain(legacyChain);
        CallerChainImpl joined2To3 =
          (CallerChainImpl) _context.MakeChainFor(actor3);
        Assert.AreEqual(conn2.BusId, joined2To3.BusId);
        Assert.AreEqual(actor3, joined2To3.Target);
        Assert.AreEqual(_entity, joined2To3.Caller.entity);
        Assert.AreEqual(conn2.Login.Value.id, joined2To3.Caller.id);
        Assert.IsTrue(joined2To3.Originators.Length == 1);
        Assert.AreEqual(actor1, joined2To3.Originators[0].entity);
      }
      finally {
        _context.ExitChain();
        _context.SetCurrentConnection(null);
        conn1.Logout();
        conn2.Logout();
        conn3.Logout();
      }
    }

    /// <summary>
    ///   Testes do método MakeChainFor com entidade inexistente
    /// </summary>
    [TestMethod]
    public void MakeChainForInexistentEntityTest() {
      // deve funcionar mesmo que a entidade não exista.
      Connection conn1 = ConnectToBus();
      const string actor1 = "actor-1";
      conn1.LoginByPassword(actor1, Crypto.TextEncoding.GetBytes(actor1),
        _domain);

      _context.SetCurrentConnection(conn1);
      try {
        CallerChain chain = _context.MakeChainFor(FakeEntity);
        Assert.AreEqual(chain.BusId, conn1.BusId);
        Assert.AreEqual(chain.Target, FakeEntity);
        Assert.AreEqual(chain.Caller.id, conn1.Login.Value.id);
        Assert.AreEqual(chain.Caller.entity, actor1);
        Assert.AreEqual(chain.Originators.Length, 0);
      }
      finally {
        _context.SetCurrentConnection(null);
      }
      conn1.Logout();
    }

    /// <summary>
    ///   Testes do método ImportChain
    /// </summary>
    [TestMethod]
    public void ImportChainTest() {
      Connection conn = ConnectToBus();
      conn.LoginByPassword(_login, _password, _domain);
      string busid = conn.BusId;
      string login = conn.Login.Value.id;
      string entity = _login;
      const string extOrig = "ExternalOriginator";
      const string extCaller = "ExternalCaller";

      _context.SetCurrentConnection(conn);
      try {
        string token = entity + "@" + login + ": " + extOrig + ", " + extCaller;
        byte[] bytes = Crypto.TextEncoding.GetBytes(token);
        CallerChain imported = _context.ImportChain(bytes, _domain);
        Assert.AreEqual(imported.BusId, busid);
        Assert.AreEqual(imported.Target, entity);
        Assert.AreEqual(imported.Caller.id, Unknown);
        Assert.AreEqual(imported.Caller.entity, extCaller);
        Assert.AreEqual(imported.Originators[0].id, Unknown);
        Assert.AreEqual(imported.Originators[0].entity, extOrig);
        Assert.AreEqual(imported.Originators.Length, 1);

        // faz chamada joined na cadeia importada
        _context.JoinChain(imported);
        CallerChain joined = _context.MakeChainFor(FakeEntity);
        _context.ExitChain();

        Assert.AreEqual(joined.BusId, busid);
        Assert.AreEqual(joined.Target, FakeEntity);
        Assert.AreEqual(joined.Caller.id, login);
        Assert.AreEqual(joined.Caller.entity, entity);
        Assert.AreEqual(joined.Originators[1].id, Unknown);
        Assert.AreEqual(joined.Originators[1].entity, extCaller);
        Assert.AreEqual(joined.Originators[0].id, Unknown);
        Assert.AreEqual(joined.Originators[0].entity, extOrig);
        Assert.AreEqual(joined.Originators.Length, 2);
      }
      finally {
        _context.SetCurrentConnection(null);
      }
      conn.Logout();
    }

    /// <summary>
    ///   Testes do método ImportChain com domínio desconhecido
    /// </summary>
    [TestMethod]
    public void ImportChainUnknownDomainTest() {
      Connection conn = ConnectToBus();
      conn.LoginByPassword(_login, _password, _domain);
      _context.SetCurrentConnection(conn);
      string login = conn.Login.Value.id;
      string entity = _login;
      const string extOrig = "ExternalOriginator";
      const string extCaller = "ExternalCaller";

      string token = entity + "@" + login + ": " + extOrig + ", " + extCaller;
      byte[] bytes = Crypto.TextEncoding.GetBytes(token);
      bool failed = false;
      try {
        _context.ImportChain(bytes, "UnknownDomain");
      }
      catch (Exception e) {
        UnknownDomain ud = null;
        if (e is TargetInvocationException) {
          // caso seja uma exceção lançada pelo SDK, será uma NO_PERMISSION
          ud = e.InnerException as UnknownDomain;
        }
        if ((ud == null) && (!(e is UnknownDomain))) {
          Assert.Fail(
            "A exceção deveria ser UnknownDomain. Exceção recebida: " + e);
        }
        ud = ud ?? (UnknownDomain) e;
        failed = true;
        Assert.AreEqual(ud.domain, "UnknownDomain");
      }
      finally {
        _context.SetCurrentConnection(null);
        conn.Logout();
      }
      Assert.IsTrue(failed);
    }

    /// <summary>
    ///   Testes do método ImportChain com token inválido
    /// </summary>
    [TestMethod]
    public void ImportChainInvalidTokenTest() {
      Connection conn = ConnectToBus();
      conn.LoginByPassword(_login, _password, _domain);
      _context.SetCurrentConnection(conn);
      byte[] bytes = Crypto.TextEncoding.GetBytes("InvalidToken");
      bool failed = false;
      try {
        _context.ImportChain(bytes, _domain);
      }
      catch (Exception e) {
        InvalidToken it = null;
        if (e is TargetInvocationException) {
          // caso seja uma exceção lançada pelo SDK, será uma NO_PERMISSION
          it = e.InnerException as InvalidToken;
        }
        if ((it == null) && (!(e is InvalidToken))) {
          Assert.Fail(
            "A exceção deveria ser InvalidToken. Exceção recebida: " + e);
        }
        failed = true;
      }
      finally {
        _context.SetCurrentConnection(null);
        conn.Logout();
      }
      Assert.IsTrue(failed);
    }

    /// <summary>
    ///   Testes com join do método ImportChain
    /// </summary>
    [TestMethod]
    public void ImportChainJoinedTest() {
      Connection conn = ConnectToBus();
      Connection conn2 = ConnectToBus();
      const string service = "service";
      const string user1 = "external_1";
      const string user2 = "external_2";
      string userChain = user1 + ", " + user2;
      _context.SetCurrentConnection(conn2);
      conn2.LoginByCertificate(_entity, _accessKey);

      // registra serviço 2
      _context.OnCallDispatch =
        (context, busid, loginId, uri, operation) => conn2;
      try {
        ComponentContext component = BuildTestCallerChainInspectorComponent();
        ServiceOffer offer =
          _context.OfferRegistry.registerService(component.GetIComponent(),
            new ServiceProperty[0]);

        // busca com serviço 1
        _context.SetCurrentConnection(conn);
        conn.LoginByPassword(service, Crypto.TextEncoding.GetBytes(service),
          _domain);
        CallerChainInspector inspector =
          (CallerChainInspector)
            offer.service_ref.getFacet(
              Repository.GetRepositoryID(typeof (CallerChainInspector)));

        // realiza chamada utilizando a cadeia com usuarios externos
        userChain = conn.Login.Value.entity + "@" + conn.Login.Value.id + ": " +
                    userChain;
        CallerChain chainUsertoService =
          _context.ImportChain(Crypto.TextEncoding.GetBytes(userChain), _domain);
        _context.JoinChain(chainUsertoService);
        String[] callers1_2 = {user1, user2, conn.Login.Value.entity};
        String[] callers = inspector.listCallers();
        Assert.AreEqual(callers1_2.Length, callers.Length);
        for (int i = 0; i < callers.Length; i++) {
          Assert.AreEqual(callers1_2[i], callers[i]);
        }
      }
      finally {
        _context.ExitChain();
        _context.SetCurrentConnection(null);
        _context.OnCallDispatch = null;
        conn.Logout();
        conn2.Logout();
      }
    }

    /// <summary>
    ///   Testes simulando chamada
    /// </summary>
    [TestMethod]
    public void SimulateCallTest() {
      Connection conn1 = ConnectToBus();
      const string actor1 = "actor-1";
      conn1.LoginByPassword(actor1, Crypto.TextEncoding.GetBytes(actor1),
        _domain);
      String login1 = conn1.Login.Value.id;
      Connection conn2 = ConnectToBus();
      const string actor2 = "actor-2";
      conn2.LoginByPassword(actor2, Crypto.TextEncoding.GetBytes(actor2),
        _domain);
      String login2 = conn2.Login.Value.id;
      Connection conn3 = ConnectToBus();
      const string actor3 = "actor-3";
      conn3.LoginByPassword(actor3, Crypto.TextEncoding.GetBytes(actor3),
        _domain);
      String login3 = conn3.Login.Value.id;

      Connection conn = ConnectToBus();
      conn.LoginByCertificate(_entity, _accessKey);

      _context.OnCallDispatch =
        (context, busid, loginId, uri, operation) => conn;

      _context.SetCurrentConnection(conn1);
      try {
        CallerChain chain1For2 = _context.MakeChainFor(conn2.Login.Value.entity);
        CallerChain chain1For3 = _context.MakeChainFor(conn3.Login.Value.entity);
        _context.SetCurrentConnection(conn2);
        _context.JoinChain(chain1For2);
        CallerChain chain1_2For3 =
          _context.MakeChainFor(conn3.Login.Value.entity);
        _context.ExitChain();

        _context.SetCurrentConnection(conn);
        ComponentContext component = BuildTestCallerChainInspectorComponent();
        ServiceOffer offer =
          _context.OfferRegistry.registerService(component.GetIComponent(),
            new ServiceProperty[0]);

        _context.SetCurrentConnection(conn3);
        CallerChainInspector inspector =
          (CallerChainInspector)
            offer.service_ref.getFacet(
              Repository.GetRepositoryID(typeof (CallerChainInspector)));

        _context.JoinChain(chain1For3);
        String[] callers1_3 = {actor1, actor3};
        String[] logins1_3 = {login1, login3};

        String[] callers = inspector.listCallers();
        Assert.AreEqual(callers1_3.Length, callers.Length);
        for (int i = 0; i < callers.Length; i++) {
          Assert.AreEqual(callers1_3[i], callers[i]);
        }

        String[] logins = inspector.listCallerLogins();
        Assert.AreEqual(logins1_3.Length, logins.Length);
        for (int i = 0; i < logins.Length; i++) {
          Assert.AreEqual(logins1_3[i], logins[i]);
        }

        _context.ExitChain();
        conn1.Logout();
        conn2.Logout();

        _context.JoinChain(chain1_2For3);
        String[] callers1_2_3 = {actor1, actor2, actor3};
        String[] logins1_2_3 = {login1, login2, login3};

        callers = inspector.listCallers();
        Assert.AreEqual(callers1_2_3.Length, callers.Length);
        for (int i = 0; i < callers.Length; i++) {
          Assert.AreEqual(callers1_2_3[i], callers[i]);
        }

        logins = inspector.listCallerLogins();
        Assert.AreEqual(logins1_2_3.Length, logins.Length);
        for (int i = 0; i < logins.Length; i++) {
          Assert.AreEqual(logins1_2_3[i], logins[i]);
        }
      }
      finally {
        _context.ExitChain();
        _context.SetCurrentConnection(null);
        _context.OnCallDispatch = null;
      }
      conn3.Logout();
      conn.Logout();
    }

    /// <summary>
    ///   Testes dos métodos EncodeChain e DecodeChain
    /// </summary>
    [TestMethod]
    public void EncodeAndDecodeChain() {
      Connection conn1 = ConnectToBus();
      const string actor1 = "actor-1";
      conn1.LoginByPassword(actor1, Crypto.TextEncoding.GetBytes(actor1),
        _domain);
      String login1 = conn1.Login.Value.id;
      Connection conn2 = ConnectToBus();
      const string actor2 = "actor-2";
      conn2.LoginByPassword(actor2, Crypto.TextEncoding.GetBytes(actor2),
        _domain);
      String login2 = conn2.Login.Value.id;
      Connection conn3 = ConnectToBus();
      const string actor3 = "actor-3";
      conn3.LoginByPassword(actor3, Crypto.TextEncoding.GetBytes(actor3),
        _domain);

      _context.SetCurrentConnection(conn1);
      try {
        CallerChain chain1For2 = _context.MakeChainFor(actor2);
        byte[] encodeChain = _context.EncodeChain(chain1For2);
        CallerChain decodedChain = _context.DecodeChain(encodeChain);
        Assert.AreEqual(conn1.BusId, decodedChain.BusId);
        Assert.AreEqual(conn2.BusId, decodedChain.BusId);
        Assert.AreEqual(actor2, decodedChain.Target);
        Assert.AreEqual(actor1, decodedChain.Caller.entity);
        Assert.AreEqual(login1, decodedChain.Caller.id);

        _context.SetCurrentConnection(conn2);
        _context.JoinChain(decodedChain);
        CallerChain chain1_2For3 = _context.MakeChainFor(actor3);

        encodeChain = _context.EncodeChain(chain1_2For3);
        decodedChain = _context.DecodeChain(encodeChain);

        Assert.AreEqual(conn1.BusId, decodedChain.BusId);
        Assert.AreEqual(conn2.BusId, decodedChain.BusId);
        Assert.AreEqual(conn3.BusId, decodedChain.BusId);
        Assert.AreEqual(actor3, decodedChain.Target);
        Assert.AreEqual(actor2, decodedChain.Caller.entity);
        Assert.AreEqual(login2, decodedChain.Caller.id);
        LoginInfo[] originators = decodedChain.Originators;
        Assert.IsTrue(originators.Length > 0);
        LoginInfo info1 = originators[0];
        Assert.AreEqual(actor1, info1.entity);
        Assert.AreEqual(login1, info1.id);
      }
      finally {
        _context.SetCurrentConnection(null);
        _context.ExitChain();
      }
      conn1.Logout();
      conn2.Logout();
      conn3.Logout();
    }

    /// <summary>
    ///   Testes dos métodos EncodeChain e DecodeChain com cadeia legada
    /// </summary>
    [TestMethod]
    public void EncodeAndDecodeLegacyChain() {
      Connection conn = ConnectToBus();
      conn.LoginByPassword(_login, _password, _domain);
      LoginInfo login = conn.Login.Value;
      const string target = "target";
      _context.SetCurrentConnection(conn);
      CallerChainImpl chain;
      string busId;
      try {
        chain = (CallerChainImpl) _context.MakeChainFor(target);
        chain.Legacy = true;
        chain.Signed.Chain = new SignedData();
        busId = conn.BusId;
      }
      finally {
        _context.SetCurrentConnection(null);
      }
      conn.Logout();
      byte[] encodeChain = _context.EncodeChain(chain);
      CallerChainImpl decodedChain =
        (CallerChainImpl) _context.DecodeChain(encodeChain);
      Assert.AreEqual(busId, decodedChain.BusId);
      Assert.AreEqual(target, decodedChain.Target);
      Assert.AreEqual(login.entity, decodedChain.Caller.entity);
      Assert.AreEqual(login.id, decodedChain.Caller.id);
      Assert.AreEqual(0, decodedChain.Originators.Length);
      Assert.IsTrue(decodedChain.Legacy);
      Assert.IsNull(decodedChain.Signed.Chain.encoded);
      Assert.IsNull(decodedChain.Signed.Chain.signature);
      Assert.IsNotNull(decodedChain.Signed.Signature);
      Assert.IsNotNull(decodedChain.Signed.Encoded);
      Assert.IsNotNull(decodedChain.Signed.LegacyChain.signature);
      Assert.IsNotNull(decodedChain.Signed.LegacyChain.encoded);
    }

    /// <summary>
    ///   Testes dos métodos EncodeSharedAuth e DecodeSharedAuth
    /// </summary>
    [TestMethod]
    public void EncodeAndDecodeSharedAuth() {
      Connection conn = ConnectToBus();
      conn.LoginByPassword(_login, _password, _domain);
      try {
        _context.SetCurrentConnection(conn);
        SharedAuthSecret secret = conn.StartSharedAuth();
        byte[] data = _context.EncodeSharedAuth(secret);
        Connection conn2 = ConnectToBus();
        SharedAuthSecret secret2 = _context.DecodeSharedAuth(data);
        conn2.LoginBySharedAuth(secret2);
        Assert.IsNotNull(conn2.Login);
        Assert.IsFalse(conn.Login.Value.id.Equals(conn2.Login.Value.id));
        Assert.AreEqual(conn.Login.Value.entity, conn2.Login.Value.entity);
      }
      finally {
        _context.SetCurrentConnection(null);
      }
    }

    /// <summary>
    ///   Teste de decodificação de uma SharedAuth como uma cadeia
    /// </summary>
    [TestMethod]
    [ExpectedException(typeof (InvalidEncodedStreamException))]
    public void DecodeSharedAuthAsChain() {
      Connection conn = ConnectToBus();
      conn.LoginByPassword(_login, _password, _domain);
      _context.SetCurrentConnection(conn);
      SharedAuthSecret secret = conn.StartSharedAuth();
      try {
        byte[] data = _context.EncodeSharedAuth(secret);
        _context.DecodeChain(data);
      }
      finally {
        secret.Cancel();
        _context.SetCurrentConnection(null);
        conn.Logout();
      }
    }

    /// <summary>
    ///   Teste de decodificação de uma cadeia como uma SharedAuth
    /// </summary>
    [TestMethod]
    [ExpectedException(typeof (InvalidEncodedStreamException))]
    public void DecodeChainAsSharedAuth() {
      Connection conn1 = ConnectToBus();
      const string actor1 = "actor-1";
      conn1.LoginByPassword(actor1, Crypto.TextEncoding.GetBytes(actor1),
        _domain);
      Connection conn2 = ConnectToBus();
      const string actor2 = "actor-2";
      conn2.LoginByPassword(actor2, Crypto.TextEncoding.GetBytes(actor2),
        _domain);
      String login2 = conn2.Login.Value.id;
      try {
        _context.SetCurrentConnection(conn1);
        CallerChain chain1For2 = _context.MakeChainFor(login2);
        byte[] data = _context.EncodeChain(chain1For2);
        _context.DecodeSharedAuth(data);
      }
      finally {
        _context.SetCurrentConnection(null);
        conn1.Logout();
        conn2.Logout();
      }
    }

    /// <summary>
    ///   Teste que utiliza dois servidores se registrando no mesmo barramento para verificar o bugg relatado em OPENBUS-2754
    ///   (uso de SSL).
    /// </summary>
    [TestMethod]
    public void MultipleServersSameBusMultithreaded() {
      Connection conn1 = ConnectToBus();
      Connection conn2 = ConnectToBus();
      MultipleServers(conn1, conn2);
    }

    /// <summary>
    ///   Teste que utiliza dois servidores se registrando em diferentes barramentos para verificar o bugg relatado em
    ///   OPENBUS-2754 (uso de SSL).
    /// </summary>
    [TestMethod]
    public void MultipleServersDifferentBusMultithreaded() {
      Connection conn2;
      Connection conn1 = ConnectToBuses(out conn2);
      MultipleServers(conn1, conn2);
    }

    /// <summary>
    ///   Teste que utiliza um servidor e dois clientes no mesmo barramento para verificar o bugg relatado em OPENBUS-2754 (uso
    ///   de SSL).
    /// </summary>
    [TestMethod]
    public void MultipleClientsSameBusMultithreaded() {
      Connection conn1 = ConnectToBus();
      conn1.LoginByCertificate(_entity, _accessKey);
      Connection conn2 = ConnectToBus();
      const string actor1 = "actor-1";
      conn2.LoginByPassword(actor1, Crypto.TextEncoding.GetBytes(actor1),
        _domain);
      Connection conn3 = ConnectToBus();
      const string actor2 = "actor-2";
      conn3.LoginByPassword(actor2, Crypto.TextEncoding.GetBytes(actor2),
        _domain);
      _context.OnCallDispatch =
        (context, busid, loginId, uri, operation) => conn1;
      try {
        // registra serviço
        Thread thread1 = new Thread(MultiplexingServerThread);
        thread1.Start(conn1);
        // aguarda o registro terminar
        if (!thread1.Join(TimeSpan.FromSeconds(Timeout))) {
          thread1.Abort();
          Assert.Fail("Registro do servidor não ocorreu corretamente em " + Timeout + " segundos.");
        }
        lock (ThreadRetLock) {
          if (_threadRet != null) {
            Assert.Fail(_threadRet.ToString());
          }
        }
        // busca serviço com conexões 2 e 3
        Thread thread2 = new Thread(MultiplexingClientThread);
        Thread thread3 = new Thread(MultiplexingClientThread);
        thread2.Start(conn2);
        thread3.Start(conn3);
        // aguarda o término
        thread2.Join();
        thread3.Join();
      }
      finally {
        _context.SetCurrentConnection(null);
        _context.OnCallDispatch = null;
        lock (ThreadRetLock) {
          _threadRet = null;
        }
        conn1.Logout();
        conn2.Logout();
        conn3.Logout();
      }
    }

    /// <summary>
    ///   Teste de multiplexação simples, somente dois clientes para um servidor e um único barramento.
    /// </summary>
    [TestMethod]
    public void Multiplexing() {
      Connection conn1 = ConnectToBus();
      const string actor1 = "actor-1";
      conn1.LoginByPassword(actor1, Crypto.TextEncoding.GetBytes(actor1),
        _domain);
      Connection conn2 = ConnectToBus();
      const string actor2 = "actor-2";
      conn2.LoginByPassword(actor2, Crypto.TextEncoding.GetBytes(actor2),
        _domain);
      Connection conn3 = ConnectToBus();
      conn3.LoginByCertificate(_entity, _accessKey);
      _context.OnCallDispatch =
        (context, busid, loginId, uri, operation) => conn3;
      try {
        // registra serviço
        _context.SetCurrentConnection(conn3);
        ComponentContext component = BuildTestCallerChainInspectorComponent();
        _context.OfferRegistry.registerService(component.GetIComponent(),
          new ServiceProperty[0]);
        // busca serviço com conexão 1
        _context.SetCurrentConnection(conn1);
        ServiceProperty[] props = {
          new ServiceProperty("openbus.component.interface",
            Repository.GetRepositoryID(typeof (CallerChainInspector)))
        };
        ServiceOfferDesc[] offers = _context.OfferRegistry.findServices(props);
        if (offers.Length == 0) {
          Assert.Fail("Não foi encontrada oferta do serviço.");
        }
        offers[0].service_ref.getComponentId();
        // busca serviço com conexão 2
        _context.SetCurrentConnection(conn2);
        offers = _context.OfferRegistry.findServices(props);
        if (offers.Length == 0) {
          Assert.Fail("Não foi encontrada oferta do serviço.");
        }
        offers[0].service_ref.getComponentId();
      }
      finally {
        _context.SetCurrentConnection(null);
        _context.OnCallDispatch = null;
        conn1.Logout();
        conn2.Logout();
        conn3.Logout();
      }
    }

    /// <summary>
    ///   Teste de multiplexação com dois clientes e dois servidores usando barramentos diferentes.
    /// </summary>
    [TestMethod]
    public void MultiplexingMultipleClientsAndServers() {
      Connection conn2;
      Connection conn1 = ConnectToBuses(out conn2);
      const string actor1 = "actor-1";
      conn1.LoginByPassword(actor1, Crypto.TextEncoding.GetBytes(actor1),
        _domain);
      const string actor2 = "actor-2";
      conn2.LoginByPassword(actor2, Crypto.TextEncoding.GetBytes(actor2),
        _domain);
      Connection conn4;
      Connection conn3 = ConnectToBuses(out conn4);
      conn3.LoginByCertificate(_entity, _accessKey);
      conn4.LoginByCertificate(_entity, _accessKey);
      _context.OnCallDispatch =
        (context, busid, loginId, uri, operation) =>
          busid.Equals(conn3.BusId) ? conn3 : conn4;
      try {
        // registra serviço 1
        _context.SetCurrentConnection(conn3);
        ComponentContext component = BuildTestCallerChainInspectorComponent();
        _context.OfferRegistry.registerService(component.GetIComponent(),
          new ServiceProperty[0]);
        // registra serviço 2
        _context.SetCurrentConnection(conn4);
        ComponentContext component2 = BuildTestCallerChainInspectorComponent();
        _context.OfferRegistry.registerService(component2.GetIComponent(),
          new ServiceProperty[0]);
        // busca serviço 1 com conexão 1
        _context.SetCurrentConnection(conn1);
        ServiceProperty[] props = {
          new ServiceProperty("openbus.component.interface",
            Repository.GetRepositoryID(typeof (CallerChainInspector)))
        };
        ServiceOfferDesc[] offers = _context.OfferRegistry.findServices(props);
        if (offers.Length == 0) {
          Assert.Fail("Não foi encontrada oferta do serviço.");
        }
        offers[0].service_ref.getComponentId();
        // busca serviço 2 com conexão 2
        _context.SetCurrentConnection(conn2);
        offers = _context.OfferRegistry.findServices(props);
        if (offers.Length == 0) {
          Assert.Fail("Não foi encontrada oferta do serviço.");
        }
        offers[0].service_ref.getComponentId();
      }
      finally {
        _context.SetCurrentConnection(null);
        _context.OnCallDispatch = null;
        conn1.Logout();
        conn2.Logout();
        conn3.Logout();
        conn4.Logout();
      }
    }

    /// <summary>
    ///   Teste de multiplexação com dois servidores e dois clientes para cada servidor, utilizando barramentos diferentes e
    ///   múltiplas threads.
    /// </summary>
    [TestMethod]
    public void MultiplexingMultipleClientsAndServersMultiThreaded() {
      Connection conn2;
      Connection conn1 = ConnectToBuses(out conn2);
      const string actor1 = "actor-1";
      conn1.LoginByPassword(actor1, Crypto.TextEncoding.GetBytes(actor1),
        _domain);
      const string actor2 = "actor-2";
      conn2.LoginByPassword(actor2, Crypto.TextEncoding.GetBytes(actor2),
        _domain);
      Connection conn4;
      Connection conn3 = ConnectToBuses(out conn4);
      const string actor3 = "actor-3";
      conn3.LoginByPassword(actor3, Crypto.TextEncoding.GetBytes(actor3),
        _domain);
      const string actor4 = "actor-4";
      conn4.LoginByPassword(actor4, Crypto.TextEncoding.GetBytes(actor4),
        _domain);
      Connection conn6;
      Connection conn5 = ConnectToBuses(out conn6);
      conn5.LoginByCertificate(_entity, _accessKey);
      conn6.LoginByCertificate(_entity, _accessKey);
      _context.OnCallDispatch =
        (context, busid, loginId, uri, operation) =>
          busid.Equals(conn5.BusId) ? conn5 : conn6;
      try {
        // registra serviços
        Thread thread1 = new Thread(MultiplexingServerThread);
        Thread thread2 = new Thread(MultiplexingServerThread);
        thread1.Start(conn5);
        thread2.Start(conn6);
        // aguarda os registros terminarem
        bool ok = true;
        if (!thread1.Join(TimeSpan.FromSeconds(Timeout))) {
          thread1.Abort();
          ok = false;
        }
        Assert.IsTrue(ok, "Registro do servidor " + 1 + " não ocorreu corretamente em " + Timeout + " segundos.");
        if (!thread2.Join(TimeSpan.FromSeconds(Timeout))) {
          thread2.Abort();
          ok = false;
        }
        Assert.IsTrue(ok, "Registro do servidor " + 2 + " não ocorreu corretamente em " + Timeout + " segundos.");
        lock (ThreadRetLock) {
          if (_threadRet != null) {
            Assert.Fail(_threadRet.ToString());
          }
        }
        // busca serviço 1 com conexões 1 e 3
        Thread thread3 = new Thread(MultiplexingClientThread);
        Thread thread4 = new Thread(MultiplexingClientThread);
        // busca serviço 2 com conexões 2 e 4
        Thread thread5 = new Thread(MultiplexingClientThread);
        Thread thread6 = new Thread(MultiplexingClientThread);
        thread3.Start(conn1);
        thread4.Start(conn3);
        thread5.Start(conn2);
        thread6.Start(conn4);
        // aguarda o término
        thread3.Join();
        thread4.Join();
        thread5.Join();
        thread6.Join();
      }
      finally {
        _context.SetCurrentConnection(null);
        _context.OnCallDispatch = null;
        lock (ThreadRetLock) {
          _threadRet = null;
        }
        conn1.Logout();
        conn2.Logout();
        conn3.Logout();
        conn4.Logout();
        conn5.Logout();
        conn6.Logout();
      }
    }

    /// <summary>
    ///   Constrói um componente que oferece faceta de inspeção de cadeia de chamadas.
    /// </summary>
    private static ComponentContext BuildTestCallerChainInspectorComponent() {
      ComponentId id = new ComponentId("TestComponent", 1, 0, 0, "csharp");
      ComponentContext component = new DefaultComponentContext(id);
      component.AddFacet("CallerChainInspector",
        Repository.GetRepositoryID(
          typeof (CallerChainInspector)),
        new CallerChainInspectorImpl());
      return component;
    }

    private static Connection ConnectToBus() {
      return _useSSL
        ? _context.ConnectByReference(_busRef, Props)
        : _context.ConnectByAddress(_hostName, _hostPort, Props);
    }

    private static Connection ConnectToBus(string host, ushort port,
      MarshalByRefObject busRef, ConnectionProperties props) {
      return _useSSL
        ? _context.ConnectByReference(busRef, props)
        : _context.ConnectByAddress(host, port, props);
    }

    private static Connection ConnectToBuses(out Connection conn2) {
      if (_useSSL) {
        conn2 = _context.ConnectByReference(_busRef2, Props);
        return _context.ConnectByReference(_busRef, Props);
      }
      conn2 = _context.ConnectByAddress(_hostName2, _hostPort2, Props);
      return _context.ConnectByAddress(_hostName, _hostPort, Props);
    }

    private static void MultiplexingServerThread(Object conn) {
      try {
        _context.SetCurrentConnection((Connection)conn);
        ComponentContext component = BuildTestCallerChainInspectorComponent();
        _context.OfferRegistry.registerService(component.GetIComponent(),
          new ServiceProperty[0]);
      }
      catch (Exception e) {
        lock (ThreadRetLock) {
          _threadRet = e;
        }
      }
    }

    private static void MultiplexingClientThread(Object conn) {
      try {
        _context.SetCurrentConnection((Connection)conn);
        ServiceProperty[] props = {
        new ServiceProperty("openbus.component.interface",
          Repository.GetRepositoryID(typeof (CallerChainInspector)))
      };
        ServiceOfferDesc[] offers = _context.OfferRegistry.findServices(props);
        offers[0].service_ref.getComponentId();
      }
      catch (Exception e) {
        lock (ThreadRetLock) {
          _threadRet = e;
        }
      }
    }

    private void MultipleServers(Connection conn1, Connection conn2) {
      conn1.LoginByCertificate(_entity, _accessKey);
      conn2.LoginByCertificate(_entity, _accessKey);
      _context.OnCallDispatch =
        (context, busid, loginId, uri, operation) =>
          busid.Equals(conn1.BusId) ? conn1 : conn2;
      try {
        // registra serviço 1
        Thread thread1 = new Thread(MultiplexingServerThread);
        Thread thread2 = new Thread(MultiplexingServerThread);
        thread1.Start(conn1);
        thread2.Start(conn2);
        // aguarda os registros terminarem
        bool ok = true;
        if (!thread1.Join(TimeSpan.FromSeconds(Timeout))) {
          thread1.Abort();
          ok = false;
        }
        Assert.IsTrue(ok, "Registro do servidor " + 1 + " não ocorreu corretamente em " + Timeout + " segundos.");
        if (!thread2.Join(TimeSpan.FromSeconds(Timeout))) {
          thread2.Abort();
          ok = false;
        }
        Assert.IsTrue(ok, "Registro do servidor " + 2 + " não ocorreu corretamente em " + Timeout + " segundos.");
        lock (ThreadRetLock) {
          if (_threadRet != null) {
            Assert.Fail(_threadRet.ToString());
          }
        }
      }
      finally {
        _context.SetCurrentConnection(null);
        _context.OnCallDispatch = null;
        lock (ThreadRetLock) {
          _threadRet = null;
        }
        conn1.Logout();
        conn2.Logout();
      }
    }
  }
}