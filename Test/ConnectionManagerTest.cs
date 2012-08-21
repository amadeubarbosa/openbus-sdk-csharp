using System;
using System.Collections.Generic;
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
    private static OpenBusContext _context;

    private static readonly IDictionary<string, string> Props =
      new Dictionary<string, string>();

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
          invalid = _context.CreateConnection("", _hostPort, Props);
        }
        catch (InvalidBusAddressException) {
        }
        finally {
          Assert.IsNull(invalid);
        }
        try {
          invalid = _context.CreateConnection(_hostName, 0, Props);
        }
        catch (InvalidBusAddressException) {
        }
        finally {
          Assert.IsNull(invalid);
        }
        // cria conexão com propriedade legacy.delegate com valor inválido
        // essa propriedade só funciona se legacy.disable for false, o que é o padrão
        IDictionary<string, string> props = new Dictionary<string, string>();
        const string delegateProp = "legacy.delegate";
        props.Add(delegateProp, String.Empty);
        bool failed = false;
        try {
          _context.CreateConnection(_hostName, _hostPort,
                                           props);
        }
        catch (InvalidPropertyValueException e) {
          Assert.AreEqual(e.Property, delegateProp);
          Assert.AreEqual(e.Value, String.Empty);
          failed = true;
        }
        finally {
          Assert.IsTrue(failed);
        }
        // cria conexão com propriedade legacy.delegate com valores válidos
        props[delegateProp] = "caller";
        Assert.IsNotNull(_context.CreateConnection(_hostName, _hostPort, props));
        props[delegateProp] = "originator";
        Assert.IsNotNull(_context.CreateConnection(_hostName, _hostPort, props));
        // cria conexão com propriedade legacy.disable com valor inválido
        const string legacyDisableProp = "legacy.disable";
        props.Add(legacyDisableProp, String.Empty);
        failed = false;
        try {
          _context.CreateConnection(_hostName, _hostPort, props);
        }
        catch (InvalidPropertyValueException e) {
          Assert.AreEqual(e.Property, legacyDisableProp);
          Assert.AreEqual(e.Value, String.Empty);
          failed = true;
        }
        finally {
          Assert.IsTrue(failed);
        }
        // cria conexão com propriedade legacy.disable true e legacy.delegate inválido
        // tem que funcionar pois legacy.delegate deve ser ignorado
        props[legacyDisableProp] = "true";
        props[delegateProp] = String.Empty;
        Assert.IsNotNull(_context.CreateConnection(_hostName, _hostPort, props));
      }
    }

    /// <summary>
    /// Testes do método GetDispatcher
    ///</summary>
    [TestMethod]
    public void GetDispatcherTest() {
      lock (_context) {
        Connection conn = _context.CreateConnection(_hostName, _hostPort, Props);
        conn.LoginByPassword(_login, _password);
        Connection conn2 = _context.CreateConnection(_hostName, _hostPort, Props);
        conn2.LoginByPassword(_login, _password);
        _context.SetDefaultConnection(conn);
        Assert.IsNull(_context.GetDispatcher(conn.BusId));
        _context.SetCurrentConnection(conn2);
        Assert.IsNull(_context.GetDispatcher(conn.BusId));
        _context.SetDispatcher(conn2);
        Assert.AreEqual(_context.GetDispatcher(conn.BusId), conn2);
        _context.ClearDispatcher(conn.BusId);
        Assert.IsNull(_context.GetDispatcher(conn2.BusId));
        _context.SetDispatcher(conn2);
        Assert.IsTrue(conn2.Logout());
        Assert.AreEqual(_context.GetDispatcher(conn.BusId), conn2);
        _context.SetCurrentConnection(null);
        Assert.IsTrue(conn.Logout());
        _context.SetDefaultConnection(null);
        _context.ClearDispatcher(conn.BusId);
      }
    }

    /// <summary>
    /// Testes do método ClearDispatcher
    ///</summary>
    [TestMethod]
    public void ClearDispatcherTest() {
      lock (_context) {
        Connection conn = _context.CreateConnection(_hostName, _hostPort, Props);
        Connection conn2 = _context.CreateConnection(_hostName, _hostPort, Props);
        conn.LoginByPassword(_login, _password);
        conn2.LoginByPassword(_login, _password);
        Connection removed = _context.ClearDispatcher(conn.BusId);
        Assert.IsNull(removed);
        _context.SetDispatcher(conn2);
        removed = _context.ClearDispatcher(conn.BusId);
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
      lock (_context) {
        Connection conn = _context.CreateConnection(_hostName, _hostPort, Props);
        conn.LoginByPassword(_login, _password);
        Connection conn2 = _context.CreateConnection(_hostName, _hostPort, Props);
        conn2.LoginByPassword(_login, _password);
        _context.SetDefaultConnection(conn);
        Assert.IsNull(_context.GetDispatcher(conn.BusId));
        _context.SetCurrentConnection(conn);
        Assert.IsNull(_context.GetDispatcher(conn.BusId));
        _context.SetDispatcher(conn2);
        Assert.AreEqual(_context.GetDispatcher(conn.BusId), conn2);
        _context.SetCurrentConnection(conn2);
        Assert.IsTrue(conn2.Logout());
        Assert.AreEqual(_context.GetDispatcher(conn.BusId), conn2);
        _context.SetCurrentConnection(null);
        Assert.IsTrue(conn.Logout());
        _context.SetDefaultConnection(null);
        _context.ClearDispatcher(conn.BusId);
      }
    }

    /// <summary>
    /// Teste da auto-propriedade DefaultConnection
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
        _context.SetDefaultConnection(conn);
        Assert.AreEqual(_context.GetDefaultConnection(), conn);
        _context.SetDispatcher(conn);
        Assert.AreEqual(_context.GetDefaultConnection(), conn);
        _context.ClearDispatcher(conn.BusId);
        Assert.IsTrue(conn.Logout());
        Assert.AreEqual(_context.GetDefaultConnection(), conn);
        _context.SetDefaultConnection(null);
      }
    }

    /// <summary>
    /// Teste da auto-propriedade Requester
    ///</summary>
    [TestMethod]
    public void RequesterTest() {
      lock (_context) {
        Connection conn = _context.CreateConnection(_hostName, _hostPort, Props);
        conn.LoginByPassword(_login, _password);
        Assert.IsNull(_context.GetCurrentConnection());
        _context.SetDefaultConnection(conn);
        _context.SetDispatcher(conn);
        Assert.IsNull(_context.GetCurrentConnection());
        _context.SetCurrentConnection(conn);
        Assert.AreEqual(_context.GetCurrentConnection(), conn);
        _context.SetDefaultConnection(null);
        _context.ClearDispatcher(conn.BusId);
        Assert.IsTrue(conn.Logout());
        Assert.AreEqual(_context.GetCurrentConnection(), conn);
        _context.SetCurrentConnection(null);

        // tentativa de chamada sem threadrequester setado
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
        // tentativa com threadrequester setado
        _context.SetCurrentConnection(conn);
        try {
          _context.OfferRegistry.findServices(props);
        }
        catch (Exception e) {
          Assert.Fail(
            "A chamada com ThreadRequester setado deveria ser bem-sucedida. Exceção recebida: " +
            e);
        }
        _context.SetCurrentConnection(null);
      }
    }
  }
}