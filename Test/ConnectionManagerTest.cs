using System;
using System.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using omg.org.CORBA;
using tecgraf.openbus.core.v2_00.services.access_control;
using tecgraf.openbus.core.v2_00.services.offer_registry;
using tecgraf.openbus.security;

namespace tecgraf.openbus.Test {
  /// <summary>
  ///This is a test class for ConnectionManagerTest and is intended
  ///to contain all ConnectionManagerTest Unit Tests
  ///</summary>
  [TestClass]
  public class ConnectionManagerTest {
    #region Fields

    private static String _hostName;
    private static short _hostPort;
    private static String _entity;
    private static string _login;
    private static byte[] _password;
    private static ConnectionManager _manager;

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
    //Use ClassCleanup to run code after all tests in a class have run
    //[ClassCleanup()]
    //public static void MyClassCleanup()
    //{
    //}
    //
    //Use TestInitialize to run code before running each test
    //[TestInitialize()]
    //public void MyTestInitialize()
    //{
    //}
    //
    //Use TestCleanup to run code after each test has run
    //[TestCleanup()]
    //public void MyTestCleanup()
    //{
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
      _hostPort = short.Parse(port);

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

      _manager = ORBInitializer.Manager;
    }

    /// <summary>
    /// Teste da auto-propriedade ORB
    ///</summary>
    [TestMethod]
    public void ORBTest() {
      lock (_manager) {
        Assert.IsNotNull(ORBInitializer.Manager.ORB);
      }
    }

    /// <summary>
    /// Testes do método CreateConnection
    ///</summary>
    [TestMethod]
    public void CreateConnectionTest() {
      lock (_manager) {
        // cria conexão válida
        Connection valid = _manager.CreateConnection(_hostName, _hostPort);
        Assert.IsNotNull(valid);
        // tenta criar conexão com hosts inválidos
        Connection invalid = null;
        try {
          invalid = _manager.CreateConnection("", _hostPort);
        }
        catch (Exception) {
        }
        finally {
          Assert.IsNull(invalid);
        }
        try {
          invalid = _manager.CreateConnection(_hostName, -1);
        }
        catch (Exception) {
        }
        finally {
          Assert.IsNull(invalid);
        }
      }
    }

    /// <summary>
    /// Testes do método GetBusDispatcher
    ///</summary>
    [TestMethod]
    public void GetBusDispatcherTest() {
      lock (_manager) {
        Connection conn = _manager.CreateConnection(_hostName, _hostPort);
        Connection conn2 = _manager.CreateConnection(_hostName, _hostPort);
        _manager.DefaultConnection = conn;
        Assert.IsNull(_manager.GetBusDispatcher(conn.BusId));
        _manager.SetupBusDispatcher(conn2);
        Assert.AreEqual(_manager.GetBusDispatcher(conn.BusId), conn2);
        _manager.RemoveBusDispatcher(conn.BusId);
        Assert.IsNull(_manager.GetBusDispatcher(conn2.BusId));
        _manager.DefaultConnection = null;
        Assert.IsNull(_manager.GetBusDispatcher(conn.BusId));
      }
    }

    /// <summary>
    /// Testes do método RemoveBusDispatcher
    ///</summary>
    [TestMethod]
    public void RemoveBusDispatcherTest() {
      lock (_manager) {
        Connection conn = _manager.CreateConnection(_hostName, _hostPort);
        Connection conn2 = _manager.CreateConnection(_hostName, _hostPort);
        Connection removed = _manager.RemoveBusDispatcher(conn.BusId);
        Assert.IsNull(removed);
        _manager.DefaultConnection = conn;
        _manager.SetupBusDispatcher(conn2);
        removed = _manager.RemoveBusDispatcher(conn.BusId);
        Assert.AreEqual(removed, conn2);
        _manager.DefaultConnection = null;
      }
    }

    /// <summary>
    /// Testes do método SetupBusDispatcher
    ///</summary>
    [TestMethod]
    public void SetupBusDispatcherTest() {
      lock (_manager) {
        Connection conn = _manager.CreateConnection(_hostName, _hostPort);
        Connection conn2 = _manager.CreateConnection(_hostName, _hostPort);
        Assert.IsNull(_manager.GetBusDispatcher(conn.BusId));
        _manager.DefaultConnection = conn;
        _manager.ThreadRequester = conn;
        Assert.IsNull(_manager.GetBusDispatcher(conn.BusId));
        _manager.SetupBusDispatcher(conn2);
        Assert.AreEqual(_manager.GetBusDispatcher(conn.BusId), conn2);
        _manager.RemoveBusDispatcher(conn.BusId);
        _manager.DefaultConnection = null;
        _manager.ThreadRequester = null;
        Assert.IsNull(_manager.GetBusDispatcher(conn.BusId));
      }
    }

    /// <summary>
    /// Teste da auto-propriedade DefaultConnection
    ///</summary>
    [TestMethod]
    public void DefaultConnectionTest() {
      lock (_manager) {
        _manager.DefaultConnection = null;
        Connection conn = _manager.CreateConnection(_hostName, _hostPort);
        Assert.IsNull(_manager.DefaultConnection);
        _manager.SetupBusDispatcher(conn);
        _manager.ThreadRequester = conn;
        Assert.IsNull(_manager.DefaultConnection);
        _manager.DefaultConnection = conn;
        Assert.AreEqual(_manager.DefaultConnection, conn);
        _manager.DefaultConnection = null;
        _manager.RemoveBusDispatcher(conn.BusId);
        _manager.ThreadRequester = null;
      }
    }

    /// <summary>
    /// Teste da auto-propriedade ThreadRequester
    ///</summary>
    [TestMethod]
    public void ThreadRequesterTest() {
      lock (_manager) {
        Connection conn = _manager.CreateConnection(_hostName, _hostPort);
        Assert.IsNull(_manager.ThreadRequester);
        _manager.SetupBusDispatcher(conn);
        _manager.DefaultConnection = conn;
        Assert.IsNull(_manager.ThreadRequester);
        _manager.ThreadRequester = conn;
        Assert.AreEqual(_manager.ThreadRequester, conn);
        _manager.DefaultConnection = null;
        _manager.RemoveBusDispatcher(conn.BusId);
        _manager.ThreadRequester = null;

        // tentativa de chamada sem threadrequester setado
        conn.LoginByPassword(_login, _password);
        Assert.AreEqual(conn, _manager.ThreadRequester);
        bool failed = false;
        ServiceProperty[] props = new[] {new ServiceProperty("a", "b")};
        _manager.ThreadRequester = null;
        try {
          conn.Offers.findServices(props);
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
        // tentativa com threadrequester setado
        _manager.ThreadRequester = conn;
        try {
          conn.Offers.findServices(props);
        }
        catch (Exception e) {
          Assert.Fail(
            "A chamada com ThreadRequester setado deveria ser bem-sucedida. Exceção recebida: " +
            e);
        }
      }
    }
  }
}