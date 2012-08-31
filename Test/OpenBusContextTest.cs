using System;
using System.Configuration;
using System.Runtime.Remoting;
using Ch.Elca.Iiop.Idl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Scs.Core;
using omg.org.CORBA;
using scs.core;
using tecgraf.openbus.core.v2_0.services.access_control;
using tecgraf.openbus.core.v2_0.services.offer_registry;
using tecgraf.openbus.exceptions;
using tecgraf.openbus.interop.simple;
using tecgraf.openbus.security;
using tecgraf.openbus.test;

namespace tecgraf.openbus.Test {
  /// <summary>
  ///This is a test class for OpenBusContextTest and is intended
  ///to contain all OpenBusContextTest Unit Tests
  ///</summary>
  [TestClass]
  public class OpenBusTest {
    #region Fields

    private static String _hostName;
    private static ushort _hostPort;
    private static String _entity;
    private static string _login;
    private static byte[] _password;
    private static PrivateKey _accessKey;
    private static OpenBusContext _context;

    private static readonly ConnectionProperties Props =
      new ConnectionPropertiesImpl();

    /// <summary>
    ///Gets or sets the test context which provides
    ///information about and functionality for the current test run.
    ///</summary>
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

      string privateKey = ConfigurationManager.AppSettings["testKeyFileName"];
      if (String.IsNullOrEmpty(password)) {
        throw new ArgumentNullException("testKeyFileName");
      }
      _accessKey = Crypto.ReadKeyFile(privateKey);
      Props.AccessKey = _accessKey;

      _context = ORBInitializer.Context;
    }

    /// <summary>
    /// Teste da auto-propriedade ORB
    ///</summary>
    [TestMethod]
    public void ORBTest() {
      lock (_context) {
        Assert.IsNotNull(ORBInitializer.Context.ORB);
      }
    }

    /// <summary>
    /// Testes do método CreateConnection
    ///</summary>
    [TestMethod]
    public void CreateConnectionTest() {
      lock (_context) {
        // cria conexão válida
        Connection valid = _context.CreateConnection(_hostName, _hostPort, Props);
        Assert.IsNotNull(valid);
        // tenta criar conexão com hosts inválidos
        Connection invalid = null;
        try {
          invalid = _context.CreateConnection(null, _hostPort, Props);
        }
        catch (ArgumentException) {
        }
        finally {
          Assert.IsNull(invalid);
        }
        try {
          invalid = _context.CreateConnection("", _hostPort, Props);
        }
        catch (ArgumentException) {
        }
        finally {
          Assert.IsNull(invalid);
        }
        try {
          invalid = _context.CreateConnection(_hostName, 0, Props);
        }
        catch (ArgumentException) {
        }
        finally {
          Assert.IsNull(invalid);
        }
        // cria conexão com propriedade legacy.delegate com valor inválido
        // essa propriedade só funciona se legacy.disable for false, o que é o padrão
        ConnectionProperties props = new ConnectionPropertiesImpl();
        bool failed = false;
        try {
          props.LegacyDelegate = String.Empty;
        }
        catch (InvalidPropertyValueException e) {
          Assert.AreEqual(e.Property, "legacy.delegate");
          failed = true;
        }
        finally {
          Assert.IsTrue(failed);
        }
        // cria conexão com propriedade legacy.delegate com valores válidos
        props.LegacyDelegate = "caller";
        Assert.IsNotNull(_context.CreateConnection(_hostName, _hostPort, props));
        props.LegacyDelegate = "originator";
        Assert.IsNotNull(_context.CreateConnection(_hostName, _hostPort, props));
        failed = false;
        try {
          // cria conexão com propriedade access.key vazia
          props.AccessKey = null;
          Assert.IsNotNull(_context.CreateConnection(_hostName, _hostPort, props));
          // tenta setar propriedade access.key inválida
          props.AccessKey = new PrivateKeyMock();
        }
        catch (InvalidPropertyValueException e) {
          Assert.AreEqual(e.Property, "access.key");
          failed = true;
        }
        finally {
          Assert.IsTrue(failed);
          props.AccessKey = _accessKey;
        }
      }
    }

    /// <summary>
    /// Testes da auto-propriedade OnCallDispatch
    ///</summary>
    [TestMethod]
    public void OnCallDispatchCallbackTest() {
      lock (_context) {
        Connection conn = _context.CreateConnection(_hostName, _hostPort, Props);
        Assert.IsNull(_context.OnCallDispatch);
        CallDispatchCallback callback = new CallDispatchCallbackImpl(conn);
        _context.OnCallDispatch = callback;
        Assert.AreEqual(callback, _context.OnCallDispatch);
        _context.OnCallDispatch = null;
        Assert.IsNull(_context.OnCallDispatch);
      }
    }

    /// <summary>
    /// Teste do DefaultConnection
    ///</summary>
    [TestMethod]
    public void DefaultConnectionTest() {
      lock (_context) {
        _context.SetDefaultConnection(null);
        Connection conn = _context.CreateConnection(_hostName, _hostPort, Props);
        conn.LoginByPassword(_login, _password);
        Assert.IsNull(_context.GetDefaultConnection());
        _context.SetCurrentConnection(conn);
        Assert.IsNull(_context.GetDefaultConnection());
        _context.SetCurrentConnection(null);
        Connection previous = _context.SetDefaultConnection(conn);
        Assert.IsNull(previous);
        Assert.AreEqual(_context.GetDefaultConnection(), conn);
        CallDispatchCallbackImpl callback = new CallDispatchCallbackImpl(conn);
        _context.OnCallDispatch = callback;
        Assert.AreEqual(_context.GetDefaultConnection(), conn);
        _context.OnCallDispatch = null;
        Assert.IsTrue(conn.Logout());
        Assert.AreEqual(_context.GetDefaultConnection(), conn);
        previous = _context.SetDefaultConnection(null);
        Assert.AreEqual(previous, conn);

        // tentativa de chamada sem current connection setado nem default connection
        conn.LoginByPassword(_login, _password);
        Assert.IsNull(_context.GetDefaultConnection());
        bool failed = false;
        ServiceProperty[] props = new[] {new ServiceProperty("a", "b")};
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
        previous = _context.SetDefaultConnection(null);
        Assert.AreEqual(previous, conn);
      }
    }

    /// <summary>
    /// Teste do CurrentConnection
    ///</summary>
    [TestMethod]
    public void CurrentConnectionTest() {
      lock (_context) {
        Connection conn = _context.CreateConnection(_hostName, _hostPort, Props);
        conn.LoginByPassword(_login, _password);
        Assert.IsNull(_context.GetCurrentConnection());
        _context.SetDefaultConnection(conn);
        CallDispatchCallbackImpl callback = new CallDispatchCallbackImpl(conn);
        _context.OnCallDispatch = callback;
        Assert.IsNull(_context.GetCurrentConnection());
        _context.SetCurrentConnection(conn);
        Assert.AreEqual(_context.GetCurrentConnection(), conn);
        _context.SetDefaultConnection(null);
        _context.OnCallDispatch = null;
        Assert.IsTrue(conn.Logout());
        Assert.AreEqual(_context.GetCurrentConnection(), conn);
        Connection previous = _context.SetCurrentConnection(null);
        Assert.AreEqual(previous, conn);

        // tentativa de chamada sem current connection setado nem default connection
        conn.LoginByPassword(_login, _password);
        Assert.IsNull(_context.GetCurrentConnection());
        bool failed = false;
        ServiceProperty[] props = new[] {new ServiceProperty("a", "b")};
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
        previous = _context.SetCurrentConnection(null);
        Assert.AreEqual(previous, conn);
      }
    }

    /// <summary>
    /// Testes da auto-propriedade CallerChain
    ///</summary>
    [TestMethod]
    public void CallerChainTest() {
      lock (_context) {
        Connection conn = _context.CreateConnection(_hostName, _hostPort, Props);
        _context.SetDefaultConnection(conn);
        Assert.IsNull(_context.CallerChain);
        //TODO: Daqui pra baixo não funciona realmente pois a chamada sayHello não passa por CORBA, mas isso é um problema do IIOP.NET especificamente e não ocorre nas outras linguagens. Não há muito problema pois os testes de interoperabilidade ja cobrem isso de forma suficiente. Para reativar esse teste é necessário comentar o catch genérico abaixo.
        try {
          const string facetName = "HelloMock";
          conn.LoginByPassword(_login, _password);
          ComponentContext component =
            new DefaultComponentContext(new ComponentId());
          component.AddFacet(facetName,
                             Repository.GetRepositoryID(typeof (Hello)),
                             new HelloMock(conn));
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
    /// Testes do método JoinChain
    ///</summary>
    [TestMethod]
    public void JoinChainTest() {
      lock (_context) {
        Connection conn = _context.CreateConnection(_hostName, _hostPort, Props);
        _context.SetCurrentConnection(conn);
        Assert.IsNull(_context.JoinedChain);
        // adiciona a chain da getCallerChain
        _context.JoinChain(null);
        Assert.IsNull(_context.JoinedChain);
        //TODO testar caso em que a chain da getCallerChain não é vazia
        //TODO não há como testar o caso do TODO acima em C# sem usar processos diferentes para o servidor e cliente. Não há muito problema pois os testes de interoperabilidade cobrem esse caso.
        conn.LoginByPassword(_login, _password);
        Assert.IsNotNull(conn.Login);
        _context.JoinChain(new CallerChainImpl("mock", new LoginInfo("a", "b"),
                                               conn.Login.Value,
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
    /// Testes do método ExitChain
    ///</summary>
    [TestMethod]
    public void ExitChainTest() {
      lock (_context) {
        Connection conn = _context.CreateConnection(_hostName, _hostPort, Props);
        _context.SetCurrentConnection(conn);
        Assert.IsNull(_context.JoinedChain);
        _context.ExitChain();
        Assert.IsNull(_context.JoinedChain);
        conn.LoginByPassword(_login, _password);
        Assert.IsNotNull(conn.Login);
        _context.JoinChain(new CallerChainImpl("mock", new LoginInfo("a", "b"),
                                               conn.Login.Value,
                                               new LoginInfo[0]));
        Assert.IsNotNull(_context.JoinedChain);
        _context.ExitChain();
        Assert.IsNull(_context.JoinedChain);
        Assert.IsTrue(conn.Logout());
        _context.SetCurrentConnection(null);
      }
    }

    //Use TestCleanup to run code after each test has run
    [TestCleanup]
    public void MyTestCleanup() {
      // não gera erro em testes rodados automaticamente mas permite perceber ao rodar na mão
      CheckConnectionsMapSize();
    }

    private void CheckConnectionsMapSize() {
      int size;
      lock (_context) {
        size = ((OpenBusContextImpl) _context).GetConnectionsMapSize();
      }
      Assert.AreEqual(0, size,
                      "Número de conexões no contexto ao final dos testes não é zero, é " +
                      size + ".");
    }
  }
}