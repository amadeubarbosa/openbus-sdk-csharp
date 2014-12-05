using System;
using System.Configuration;
using System.Reflection;
using System.Runtime.Remoting;
using Ch.Elca.Iiop.Idl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using omg.org.CORBA;
using Scs.Core;
using scs.core;
using tecgraf.openbus.core.v2_1;
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
  ///   to contain all OpenBusContextTest Unit Tests
  /// </summary>
  [TestClass]
  public class OpenBusTest {
    #region Fields

    private static String _hostName;
    private static ushort _hostPort;
    private static String _entity;
    private static string _login;
    private static byte[] _password;
    private static string _domain;
    private static PrivateKey _accessKey;
    private static OpenBusContext _context;
    private static readonly Object Lock = new Object();
    private const string FakeEntity = "Fake Entity";
    private const string Unknown = "<unknown>";

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
    //Use TestInitialize to run code before running each test
    //[TestInitialize()]
    //public void MyTestInitialize()
    //{
    //}
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

    #endregion

    //Use ClassInitialize to run code before running the first test in the class
    [ClassInitialize]
    public static void MyClassInitialize(TestContext testContext) {
      _hostName = ConfigurationManager.AppSettings["hostName"];
      if (String.IsNullOrEmpty(_hostName)) {
        throw new ArgumentNullException("hostName");
      }

      string port = ConfigurationManager.AppSettings["hostPort"];
      _hostPort = ushort.Parse(port);

      _entity = ConfigurationManager.AppSettings["entityName"];
      if (String.IsNullOrEmpty(_entity)) {
        throw new ArgumentNullException("entityName");
      }

      _login = ConfigurationManager.AppSettings["userLogin"];
      if (String.IsNullOrEmpty(_login)) {
        throw new ArgumentNullException("userLogin");
      }

      string password = ConfigurationManager.AppSettings["userPassword"];
      if (String.IsNullOrEmpty(password)) {
        throw new ArgumentNullException("userPassword");
      }
      _password = Crypto.TextEncoding.GetBytes(password);

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

      ORBInitializer.InitORB();
      _context = ORBInitializer.Context;
    }

    /// <summary>
    ///   Teste da auto-propriedade ORB
    /// </summary>
    [TestMethod]
    public void ORBTest() {
      lock (Lock) {
        Assert.IsNotNull(ORBInitializer.Context.ORB);
      }
    }

    /// <summary>
    ///   Testes do método CreateConnection
    /// </summary>
    [TestMethod]
    public void CreateConnectionTest() {
      lock (Lock) {
        // cria conexão válida
        Assert.IsNotNull(_context.ConnectByAddress(_hostName, _hostPort, Props));
        // cria conexão válida por referência
        String corbaloc = "corbaloc::1.0@" + _hostName + ":" + _hostPort + "/" +
                          BusObjectKey.ConstVal;
        IComponent reference =
          (IComponent) RemotingServices.Connect(typeof (IComponent), corbaloc);
        Assert.IsNotNull(_context.ConnectByReference(reference, Props));
        // tenta criar conexão com hosts inválidos
        Connection invalid = null;
        try {
          invalid = _context.ConnectByAddress(null, _hostPort, Props);
        }
        catch (ArgumentException) {
        }
        finally {
          Assert.IsNull(invalid);
        }
        try {
          invalid = _context.ConnectByAddress("", _hostPort, Props);
        }
        catch (ArgumentException) {
        }
        finally {
          Assert.IsNull(invalid);
        }
        try {
          invalid = _context.ConnectByAddress(_hostName, 0, Props);
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
      lock (Lock) {
        Connection conn = _context.ConnectByAddress(_hostName, _hostPort, Props);
        Assert.IsNull(_context.OnCallDispatch);
        CallDispatchCallbackImpl callback = new CallDispatchCallbackImpl(conn);
        _context.OnCallDispatch = callback.Dispatch;
        Assert.AreEqual(callback.Dispatch, _context.OnCallDispatch);
        _context.OnCallDispatch = null;
        Assert.IsNull(_context.OnCallDispatch);
      }
    }

    /// <summary>
    ///   Teste do DefaultConnection
    /// </summary>
    [TestMethod]
    public void DefaultConnectionTest() {
      lock (Lock) {
        _context.SetDefaultConnection(null);
        Connection conn = _context.ConnectByAddress(_hostName, _hostPort, Props);
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
    }

    /// <summary>
    ///   Teste do CurrentConnection
    /// </summary>
    [TestMethod]
    public void CurrentConnectionTest() {
      lock (Lock) {
        Connection conn = _context.ConnectByAddress(_hostName, _hostPort, Props);
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
    }

    /// <summary>
    ///   Testes da auto-propriedade CallerChain
    /// </summary>
    [TestMethod]
    public void CallerChainTest() {
      lock (Lock) {
        Connection conn = _context.ConnectByAddress(_hostName, _hostPort, Props);
        _context.SetDefaultConnection(conn);
        Assert.IsNull(_context.CallerChain);
        //TODO: Daqui pra baixo não funciona realmente pois a chamada sayHello não passa por CORBA, mas isso é um problema do IIOP.NET especificamente e não ocorre nas outras linguagens. Não há muito problema pois os testes de interoperabilidade ja cobrem isso de forma suficiente. Para reativar esse teste é necessário comentar o catch genérico abaixo.
        try {
          const string facetName = "HelloMock";
          conn.LoginByPassword(_login, _password, _domain);
          ComponentContext component =
            new DefaultComponentContext(new ComponentId());
          component.AddFacet(facetName,
            Repository.GetRepositoryID(typeof (Hello)),
            new HelloMock());
          Hello hello = component.GetFacetByName(facetName).Reference as Hello;
          Assert.IsNotNull(hello);
          hello.sayHello();
        }
        catch (UNKNOWN) {
          Assert.Fail("A cadeia obtida é nula ou não é a esperada.");
        }
          //TODO remover para reativar o teste
        catch (NullReferenceException) {
        }
          //TODO remover para reativar o teste
        catch (InvalidOperationException) {
        }
        finally {
          _context.SetDefaultConnection(null);
          conn.Logout();
        }
      }
    }

    /// <summary>
    ///   Testes do método JoinChain
    /// </summary>
    [TestMethod]
    public void JoinChainTest() {
      lock (Lock) {
        Connection conn = _context.ConnectByAddress(_hostName, _hostPort, Props);
        _context.SetCurrentConnection(conn);
        Assert.IsNull(_context.JoinedChain);
        // adiciona a chain da getCallerChain
        _context.JoinChain(null);
        Assert.IsNull(_context.JoinedChain);
        //TODO testar caso em que a chain da getCallerChain não é vazia
        //TODO não há como testar o caso do TODO acima em C# sem usar processos diferentes para o servidor e cliente. Não há muito problema pois os testes de interoperabilidade cobrem esse caso.
        conn.LoginByPassword(_login, _password, _domain);
        Assert.IsNotNull(conn.Login);
        _context.JoinChain(new CallerChainImpl("mock", new LoginInfo("a", "b"),
          conn.Login.Value.entity,
          new LoginInfo[0]));
        Assert.IsNotNull(_context.JoinedChain);
        Assert.AreEqual("mock", _context.JoinedChain.BusId);
        Assert.AreEqual("a", _context.JoinedChain.Caller.id);
        Assert.AreEqual("b", _context.JoinedChain.Caller.entity);
        _context.ExitChain();
        Assert.IsNull(_context.JoinedChain);
        Assert.IsTrue(conn.Logout());
        _context.SetCurrentConnection(null);
      }
    }

    /// <summary>
    ///   Testes do método ExitChain
    /// </summary>
    [TestMethod]
    public void ExitChainTest() {
      lock (Lock) {
        Connection conn = _context.ConnectByAddress(_hostName, _hostPort, Props);
        _context.SetCurrentConnection(conn);
        Assert.IsNull(_context.JoinedChain);
        _context.ExitChain();
        Assert.IsNull(_context.JoinedChain);
        conn.LoginByPassword(_login, _password, _domain);
        Assert.IsNotNull(conn.Login);
        _context.JoinChain(new CallerChainImpl("mock", new LoginInfo("a", "b"),
          conn.Login.Value.entity,
          new LoginInfo[0]));
        Assert.IsNotNull(_context.JoinedChain);
        _context.ExitChain();
        Assert.IsNull(_context.JoinedChain);
        Assert.IsTrue(conn.Logout());
        _context.SetCurrentConnection(null);
      }
    }

    /// <summary>
    ///   Testes do método MakeChainFor
    /// </summary>
    [TestMethod]
    public void MakeChainForTest() {
      lock (Lock) {
        Connection conn1 = _context.ConnectByAddress(_hostName, _hostPort, Props);
        const string actor1 = "actor-1";
        conn1.LoginByPassword(actor1, Crypto.TextEncoding.GetBytes(actor1),
          _domain);
        Connection conn2 = _context.ConnectByAddress(_hostName, _hostPort, Props);
        const string actor2 = "actor-2";
        conn2.LoginByPassword(actor2, Crypto.TextEncoding.GetBytes(actor2),
          _domain);

        _context.SetCurrentConnection(conn1);
        CallerChain chain1To2 = _context.MakeChainFor(conn2.Login.Value.entity);
        Assert.AreEqual(actor2, chain1To2.Target);
        Assert.AreEqual(actor1, chain1To2.Caller.entity);
        Assert.AreEqual(conn1.Login.Value.id, chain1To2.Caller.id);

        _context.SetCurrentConnection(null);
        conn1.Logout();
        conn2.Logout();
      }
    }

    /// <summary>
    ///   Testes com join do método MakeChainFor
    /// </summary>
    [TestMethod]
    public void MakeChainForJoinedTest() {
      lock (Lock) {
        Connection conn1 = _context.ConnectByAddress(_hostName, _hostPort, Props);
        const string actor1 = "actor-1";
        conn1.LoginByPassword(actor1, Crypto.TextEncoding.GetBytes(actor1),
          _domain);
        Connection conn2 = _context.ConnectByAddress(_hostName, _hostPort, Props);
        const string actor2 = "actor-2";
        conn2.LoginByPassword(actor2, Crypto.TextEncoding.GetBytes(actor2),
          _domain);
        Connection conn3 = _context.ConnectByAddress(_hostName, _hostPort, Props);
        const string actor3 = "actor-3";
        conn3.LoginByPassword(actor3, Crypto.TextEncoding.GetBytes(actor3),
          _domain);

        _context.SetCurrentConnection(conn1);
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
        _context.SetCurrentConnection(null);
        conn1.Logout();
        conn2.Logout();
        conn3.Logout();
      }
    }

    /// <summary>
    ///   Testes com join em cadeia legada do método MakeChainFor
    /// </summary>
    [TestMethod]
    [ExpectedException(typeof (NO_PERMISSION))]
    public void MakeChainForJoinedLegacyTest() {
      //TODO corrigir para legacy 2.0
      throw new NotImplementedException();
      /*
      lock (Lock) {
        Connection conn1 = _context.CreateConnection(_hostName, _hostPort, Props);
        const string actor1 = "actor-1";
        conn1.LoginByPassword(actor1, Crypto.TextEncoding.GetBytes(actor1), _domain);
        Connection conn2 = _context.CreateConnection(_hostName, _hostPort, Props);
        const string actor2 = "actor-2";
        conn2.LoginByPassword(actor2, Crypto.TextEncoding.GetBytes(actor2), _domain);
        Connection conn3 = _context.CreateConnection(_hostName, _hostPort, Props);
        const string actor3 = "actor-3";
        conn3.LoginByPassword(actor3, Crypto.TextEncoding.GetBytes(actor3), _domain);

        try {
          const string deleg = "";
          CallerChain legacyChain =
            BuildFakeLegacyCallChain(conn1.BusId, conn1.Login.Value, actor2, deleg);
          _context.SetCurrentConnection(conn2);
          _context.JoinChain(legacyChain);
          CallerChain joined2To3 = _context.MakeChainFor(conn3.Login.Value.entity);
        }
        finally {
          _context.ExitChain();
          conn1.Logout();
          conn2.Logout();
          conn3.Logout();
        }
      }
       */
    }

    /// <summary>
    ///   Testes com join em cadeia legada com delegate do método MakeChainFor
    /// </summary>
    [TestMethod]
    public void MakeChainForJoinedLegacyWithDelegateTest() {
      //TODO corrigir para legacy 2.0
      throw new NotImplementedException();
      /*
      lock (Lock) {
        Connection conn1 = _context.CreateConnection(_hostName, _hostPort, Props);
        const string actor1 = "actor-1";
        conn1.LoginByPassword(actor1, Crypto.TextEncoding.GetBytes(actor1), _domain);
        Connection conn2 = _context.CreateConnection(_hostName, _hostPort, Props);
        conn2.LoginByCertificate(_entity, _accessKey);
        Connection conn3 = _context.CreateConnection(_hostName, _hostPort, Props);
        const string actor3 = "actor-3";
        conn3.LoginByPassword(actor3, Crypto.TextEncoding.GetBytes(actor3), _domain);

        try {
          string deleg = "user1";
          CallerChain legacyChain =
            BuildFakeLegacyCallChain(conn1.BusId, conn1.Login.Value, _entity,
              deleg);
          _context.SetCurrentConnection(conn2);
          _context.JoinChain(legacyChain);
          CallerChain joined2To3 = _context.MakeChainFor(conn3.Login.Value.entity);
          Assert.AreEqual(conn2.BusId, joined2To3.BusId);
          Assert.AreEqual(actor3, joined2To3.Target);
          Assert.AreEqual(_entity, joined2To3.Caller.entity);
          Assert.AreEqual(conn2.Login.Value.id, joined2To3.Caller.id);
          Assert.IsTrue(joined2To3.Originators.Length > 0);
          Assert.AreEqual(actor1, joined2To3.Originators[0].entity);
          Assert.AreEqual(ConnectionImpl.LegacyOriginatorId, joined2To3.Originators[0].id);
        }
        finally {
          _context.ExitChain();
          conn1.Logout();
          conn2.Logout();
          conn3.Logout();
        }
      }
       */
    }

    /// <summary>
    ///   Testes do método MakeChainFor com entidade inexistente
    /// </summary>
    [TestMethod]
    public void MakeChainForInexistentEntityTest() {
      // deve funcionar mesmo que a entidade não exista.
      lock (Lock) {
        Connection conn1 = _context.ConnectByAddress(_hostName, _hostPort, Props);
        const string actor1 = "actor-1";
        conn1.LoginByPassword(actor1, Crypto.TextEncoding.GetBytes(actor1),
          _domain);

        _context.SetCurrentConnection(conn1);
        CallerChain chain = _context.MakeChainFor(FakeEntity);
        Assert.AreEqual(chain.BusId, conn1.BusId);
        Assert.AreEqual(chain.Target, FakeEntity);
        Assert.AreEqual(chain.Caller.id, conn1.Login.Value.id);
        Assert.AreEqual(chain.Caller.entity, actor1);
        Assert.AreEqual(chain.Originators.Length, 0);
        conn1.Logout();
        _context.SetCurrentConnection(null);
      }
    }

    /// <summary>
    ///   Testes do método ImportChain
    /// </summary>
    [TestMethod]
    public void ImportChainTest() {
      lock (Lock) {
        Connection conn = _context.ConnectByAddress(_hostName, _hostPort);
        conn.LoginByPassword(_login, _password, _domain);
        string busid = conn.BusId;
        string login = conn.Login.Value.id;
        string entity = _login;
        const string extOrig = "ExternalOriginator";
        const string extCaller = "ExternalCaller";

        _context.SetCurrentConnection(conn);
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

        _context.SetCurrentConnection(null);
        conn.Logout();
      }
    }

    /// <summary>
    ///   Testes do método ImportChain com domínio desconhecido
    /// </summary>
    [TestMethod]
    public void ImportChainUnknownDomainTest() {
      lock (Lock) {
        Connection conn = _context.ConnectByAddress(_hostName, _hostPort);
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
          ud = ud ?? e as UnknownDomain;
          failed = true;
          Assert.AreEqual(ud.domain, "UnknownDomain");
        }
        finally {
          _context.SetCurrentConnection(null);
          conn.Logout();
        }
        Assert.IsTrue(failed);
      }
    }

    /// <summary>
    ///   Testes do método ImportChain com token inválido
    /// </summary>
    [TestMethod]
    public void ImportChainInvalidTokenTest() {
      lock (Lock) {
        Connection conn = _context.ConnectByAddress(_hostName, _hostPort, Props);
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
    }

    /// <summary>
    ///   Testes com join do método ImportChain
    /// </summary>
    [TestMethod]
    public void ImportChainJoinedTest() {
      lock (Lock) {
        Connection conn = _context.ConnectByAddress(_hostName, _hostPort);
        Connection conn2 = _context.ConnectByAddress(_hostName, _hostPort);
        const string service = "service";
        const string user1 = "external_1";
        const string user2 = "external_2";
        string userChain = user1 + ", " + user2;
        _context.SetCurrentConnection(conn2);
        conn2.LoginByCertificate(_entity, _accessKey);

        // registra serviço 2
        _context.OnCallDispatch =
          (context, busid, loginId, uri, operation) => conn2;
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
        userChain = conn.Login.Value.entity + "@" + conn.Login.Value.id + ": " + userChain;
        CallerChain chainUsertoService =
          _context.ImportChain(Crypto.TextEncoding.GetBytes(userChain), _domain);
        _context.JoinChain(chainUsertoService);
        String[] callers1_2 = {user1, user2, conn.Login.Value.entity};
        String[] callers = inspector.listCallers();
        Assert.AreEqual(callers1_2.Length, callers.Length);
        for (int i = 0; i < callers.Length; i++) {
          Assert.AreEqual(callers1_2[i], callers[i]);
        }

        _context.ExitChain();
        _context.SetCurrentConnection(null);
        conn.Logout();
        conn2.Logout();
      }
    }

    /// <summary>
    ///   Testes simulando chamada
    /// </summary>
    [TestMethod]
    public void SimulateCallTest() {
      lock (Lock) {
        Connection conn1 = _context.ConnectByAddress(_hostName, _hostPort, Props);
        const string actor1 = "actor-1";
        conn1.LoginByPassword(actor1, Crypto.TextEncoding.GetBytes(actor1),
          _domain);
        String login1 = conn1.Login.Value.id;
        Connection conn2 = _context.ConnectByAddress(_hostName, _hostPort, Props);
        const string actor2 = "actor-2";
        conn2.LoginByPassword(actor2, Crypto.TextEncoding.GetBytes(actor2),
          _domain);
        String login2 = conn2.Login.Value.id;
        Connection conn3 = _context.ConnectByAddress(_hostName, _hostPort, Props);
        const string actor3 = "actor-3";
        conn3.LoginByPassword(actor3, Crypto.TextEncoding.GetBytes(actor3),
          _domain);
        String login3 = conn3.Login.Value.id;

        Connection conn = _context.ConnectByAddress(_hostName, _hostPort, Props);
        conn.LoginByCertificate(_entity, _accessKey);

        _context.OnCallDispatch =
          (context, busid, loginId, uri, operation) => conn;

        _context.SetCurrentConnection(conn1);
        CallerChain chain1For2 = _context.MakeChainFor(conn2.Login.Value.entity);
        CallerChain chain1For3 = _context.MakeChainFor(conn3.Login.Value.entity);
        _context.SetCurrentConnection(conn2);
        _context.JoinChain(chain1For2);
        CallerChain chain1_2For3 = _context.MakeChainFor(conn3.Login.Value.entity);
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

        _context.ExitChain();
        _context.SetCurrentConnection(null);
        _context.OnCallDispatch = null;
        conn3.Logout();
        conn.Logout();
      }
    }

    /// <summary>
    ///   Testes dos métodos EncodeChain e DecodeChain
    /// </summary>
    [TestMethod]
    public void EncodeAndDecodeChain() {
      lock (Lock) {
        Connection conn1 = _context.ConnectByAddress(_hostName, _hostPort, Props);
        const string actor1 = "actor-1";
        conn1.LoginByPassword(actor1, Crypto.TextEncoding.GetBytes(actor1),
          _domain);
        String login1 = conn1.Login.Value.id;
        Connection conn2 = _context.ConnectByAddress(_hostName, _hostPort, Props);
        const string actor2 = "actor-2";
        conn2.LoginByPassword(actor2, Crypto.TextEncoding.GetBytes(actor2),
          _domain);
        String login2 = conn2.Login.Value.id;
        Connection conn3 = _context.ConnectByAddress(_hostName, _hostPort, Props);
        const string actor3 = "actor-3";
        conn3.LoginByPassword(actor3, Crypto.TextEncoding.GetBytes(actor3),
          _domain);
        String login3 = conn3.Login.Value.id;

        _context.SetCurrentConnection(conn1);
        CallerChain chain1For2 = _context.MakeChainFor(login2);

        byte[] encodeChain = _context.EncodeChain(chain1For2);
        CallerChain decodedChain = _context.DecodeChain(encodeChain);
        Assert.AreEqual(conn1.BusId, decodedChain.BusId);
        Assert.AreEqual(conn2.BusId, decodedChain.BusId);
        Assert.AreEqual(actor2, decodedChain.Target);
        Assert.AreEqual(actor1, decodedChain.Caller.entity);
        Assert.AreEqual(login1, decodedChain.Caller.id);

        _context.SetCurrentConnection(conn2);
        _context.JoinChain(decodedChain);
        CallerChain chain1_2For3 = _context.MakeChainFor(login3);

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

        _context.SetCurrentConnection(null);
        _context.ExitChain();
        conn1.Logout();
        conn2.Logout();
        conn3.Logout();
      }
    }

    /// <summary>
    ///   Testes dos métodos EncodeChain e DecodeChain com cadeia legada
    /// </summary>
    [TestMethod]
    public void EncodeAndDecodeLegacyChain() {
      //TODO corrigir para legacy 2.0
      throw new NotImplementedException();
      /*
      lock (Lock) {
        Connection conn = _context.CreateConnection(_hostName, _hostPort, Props);
        conn.LoginByPassword(_login, _password, _domain);
        LoginInfo login = conn.Login.Value;
        string busid = conn.BusId;
        conn.Logout();
        string deleg = "";
        const string target = "anyone";
        CallerChain legacyChain =
          BuildFakeLegacyCallChain(busid, login, target, deleg);
        byte[] encodeChain = _context.EncodeChain(legacyChain);
        CallerChain decodedChain = _context.DecodeChain(encodeChain);
        Assert.AreEqual(busid, decodedChain.BusId);
        Assert.AreEqual(target, decodedChain.Target);
        Assert.AreEqual(login.entity, decodedChain.Caller.entity);
        Assert.AreEqual(login.id, decodedChain.Caller.id);
        Assert.AreEqual(0, decodedChain.Originators.Length);

        deleg = "user-origin";
        legacyChain = BuildFakeLegacyCallChain(busid, login, target, deleg);
        byte[] encodedChainDelegated = _context.EncodeChain(legacyChain);
        CallerChain delegatedChain = _context.DecodeChain(encodedChainDelegated);
        Assert.AreEqual(busid, delegatedChain.BusId);
        Assert.AreEqual(target, delegatedChain.Target);
        Assert.AreEqual(login.entity, delegatedChain.Caller.entity);
        Assert.AreEqual(login.id, delegatedChain.Caller.id);
        Assert.AreEqual(1, delegatedChain.Originators.Length);
        Assert.AreEqual(deleg, delegatedChain.Originators[0].entity);
        Assert.AreEqual(ConnectionImpl.LegacyOriginatorId,
          delegatedChain.Originators[0].id);
      }
       */
    }

    /// <summary>
    ///   Testes dos métodos EncodeSharedAuth e DecodeSharedAuth
    /// </summary>
    [TestMethod]
    public void EncodeAndDecodeSharedAuth() {
      lock (Lock) {
        Connection conn = _context.ConnectByAddress(_hostName, _hostPort, Props);
        conn.LoginByPassword(_login, _password, _domain);
        try {
          _context.SetCurrentConnection(conn);
          SharedAuthSecret secret = conn.StartSharedAuth();
          byte[] data = _context.EncodeSharedAuth(secret);
          Connection conn2 = _context.ConnectByAddress(_hostName, _hostPort, Props);
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
    }

    /// <summary>
    ///   Teste de decodificação de uma SharedAuth como uma cadeia
    /// </summary>
    [TestMethod]
    [ExpectedException(typeof (InvalidEncodedStreamException))]
    public void DecodeSharedAuthAsChain() {
      lock (Lock) {
        Connection conn = _context.ConnectByAddress(_hostName, _hostPort, Props);
        conn.LoginByPassword(_login, _password, _domain);
        _context.SetCurrentConnection(conn);
        SharedAuthSecret secret = conn.StartSharedAuth();
        try {
          byte[] data = _context.EncodeSharedAuth(secret);
          _context.DecodeChain(data);
        }
        finally {
          secret.Cancel();
          conn.Logout();
          _context.SetCurrentConnection(null);
        }
      }
    }

    /// <summary>
    ///   Teste de decodificação de uma cadeia como uma SharedAuth
    /// </summary>
    [TestMethod]
    [ExpectedException(typeof (InvalidEncodedStreamException))]
    public void DecodeChainAsSharedAuth() {
      lock (Lock) {
        Connection conn1 = _context.ConnectByAddress(_hostName, _hostPort, Props);
        const string actor1 = "actor-1";
        conn1.LoginByPassword(actor1, Crypto.TextEncoding.GetBytes(actor1),
          _domain);
        Connection conn2 = _context.ConnectByAddress(_hostName, _hostPort, Props);
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

    /// <summary>
    ///   Constrói uma cadeia legacy simulada.
    /// </summary>
    private static CallerChain BuildFakeLegacyCallChain(string busId,
      LoginInfo caller,
      string target, string deleg) {
      //TODO provavelmente não será necessário para legacy 2.0
      throw new NotImplementedException();
      /*
      LoginInfo[] originators;
      if (deleg != null) {
        originators = new LoginInfo[1];
        originators[0] = new LoginInfo(ConnectionImpl.LegacyOriginatorId, deleg);
      }
      else {
        originators = new LoginInfo[0];
      }
      CallChain callChain = new CallChain(target, originators, caller);
      return new CallerChainImpl(busId, callChain.caller, callChain.target, callChain.originators);
       */
    }
  }
}