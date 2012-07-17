using System;
using System.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using omg.org.CORBA;
using tecgraf.openbus.core.v2_0.services.access_control;
using tecgraf.openbus.core.v2_0.services.offer_registry;
using tecgraf.openbus.exceptions;
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
    private static ushort _hostPort;
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
          invalid = _manager.CreateConnection(_hostName, 0);
        }
        catch (Exception) {
        }
        finally {
          Assert.IsNull(invalid);
        }
      }
    }

    /// <summary>
    /// Testes do método GetDispatcher
    ///</summary>
    [TestMethod]
    public void GetDispatcherTest() {
      lock (_manager) {
        Connection conn = _manager.CreateConnection(_hostName, _hostPort);
        conn.LoginByPassword(_login, _password);
        Connection conn2 = _manager.CreateConnection(_hostName, _hostPort);
        conn2.LoginByPassword(_login, _password);
        _manager.DefaultConnection = conn;
        Assert.IsNull(_manager.GetDispatcher(conn.BusId));
        _manager.Requester = conn2;
        Assert.IsNull(_manager.GetDispatcher(conn.BusId));
        _manager.SetDispatcher(conn2);
        Assert.AreEqual(_manager.GetDispatcher(conn.BusId), conn2);
        _manager.ClearDispatcher(conn.BusId);
        Assert.IsNull(_manager.GetDispatcher(conn2.BusId));
        _manager.SetDispatcher(conn2);
        Assert.IsTrue(conn2.Logout());
        Assert.AreEqual(_manager.GetDispatcher(conn.BusId), conn2);
        _manager.Requester = null;
        Assert.IsTrue(conn.Logout());
        _manager.DefaultConnection = null;
        _manager.ClearDispatcher(conn.BusId);
      }
    }

    /// <summary>
    /// Testes do método ClearDispatcher
    ///</summary>
    [TestMethod]
    public void ClearDispatcherTest() {
      lock (_manager) {
        Connection conn = _manager.CreateConnection(_hostName, _hostPort);
        Connection conn2 = _manager.CreateConnection(_hostName, _hostPort);
        conn.LoginByPassword(_login, _password);
        conn2.LoginByPassword(_login, _password);
        Connection removed = _manager.ClearDispatcher(conn.BusId);
        Assert.IsNull(removed);
        _manager.SetDispatcher(conn2);
        removed = _manager.ClearDispatcher(conn.BusId);
        Assert.AreEqual(removed, conn2);
        Assert.IsTrue(conn.Logout());
        Assert.IsTrue(conn2.Logout());
      }
    }

    /// <summary>
    /// Testes do método SetDispatcher
    ///</summary>
    [TestMethod]
    public void SetDispatcherTest() {
      lock (_manager) {
        Connection conn = _manager.CreateConnection(_hostName, _hostPort);
        conn.LoginByPassword(_login, _password);
        Connection conn2 = _manager.CreateConnection(_hostName, _hostPort);
        conn2.LoginByPassword(_login, _password);
        _manager.DefaultConnection = conn;
        Assert.IsNull(_manager.GetDispatcher(conn.BusId));
        _manager.Requester = conn;
        Assert.IsNull(_manager.GetDispatcher(conn.BusId));
        _manager.SetDispatcher(conn2);
        Assert.AreEqual(_manager.GetDispatcher(conn.BusId), conn2);
        _manager.Requester = conn2;
        Assert.IsTrue(conn2.Logout());
        Assert.AreEqual(_manager.GetDispatcher(conn.BusId), conn2);
        _manager.Requester = null;
        Assert.IsTrue(conn.Logout());
        _manager.DefaultConnection = null;
        _manager.ClearDispatcher(conn.BusId);
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
        conn.LoginByPassword(_login, _password);
        Assert.IsNull(_manager.DefaultConnection);
        _manager.Requester = conn;
        Assert.IsNull(_manager.DefaultConnection);
        _manager.Requester = null;
        _manager.DefaultConnection = conn;
        Assert.AreEqual(_manager.DefaultConnection, conn);
        _manager.SetDispatcher(conn);
        Assert.AreEqual(_manager.DefaultConnection, conn);
        _manager.ClearDispatcher(conn.BusId);
        Assert.IsTrue(conn.Logout());
        Assert.AreEqual(_manager.DefaultConnection, conn);
        _manager.DefaultConnection = null;
      }
    }

    /// <summary>
    /// Teste da auto-propriedade Requester
    ///</summary>
    [TestMethod]
    public void RequesterTest() {
      lock (_manager) {
        Connection conn = _manager.CreateConnection(_hostName, _hostPort);
        conn.LoginByPassword(_login, _password);
        Assert.IsNull(_manager.Requester);
        _manager.DefaultConnection = conn;
        _manager.SetDispatcher(conn);
        Assert.IsNull(_manager.Requester);
        _manager.Requester = conn;
        Assert.AreEqual(_manager.Requester, conn);
        _manager.DefaultConnection = null;
        _manager.ClearDispatcher(conn.BusId);
        Assert.IsTrue(conn.Logout());
        Assert.AreEqual(_manager.Requester, conn);
        _manager.Requester = null;

        // tentativa de chamada sem threadrequester setado
        conn.LoginByPassword(_login, _password);
        Assert.IsNull(_manager.Requester);
        bool failed = false;
        ServiceProperty[] props = new[] {new ServiceProperty("a", "b")};
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
        _manager.Requester = conn;
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