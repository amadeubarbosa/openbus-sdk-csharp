using System.Configuration;
using System.IO;
using omg.org.CORBA;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using tecgraf.openbus.core.v2_00.services.access_control;
using tecgraf.openbus.core.v2_00.services.offer_registry;
using tecgraf.openbus.exceptions;
using tecgraf.openbus.security;

namespace tecgraf.openbus.Test {
  /// <summary>
  ///This is a test class for ConnectionTest and is intended
  ///to contain all ConnectionTest Unit Tests
  ///</summary>
  [TestClass]
  public class ConnectionTest {
    #region Fields

    private static String _hostName;
    private static short _hostPort;
    private static String _entity;
    private static String _entityNoCert;
    private static string _login;
    private static byte[] _password;
    private static byte[] _privKey;
    private static byte[] _wrongKey;
    private static ConnectionManager _manager;

    /// <summary>
    ///Gets or sets the test context which provides
    ///information about and functionality for the current test run.
    ///</summary>
    public TestContext TestContext { get; set; }

    #endregion

    #region Additional test attributes

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

      _entityNoCert = ConfigurationManager.AppSettings["entityWithoutCert"];
      if (String.IsNullOrEmpty(_entity)) {
        throw new ArgumentNullException("entityWithoutCert");
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
      _privKey = File.ReadAllBytes(privateKey);

      string wrongKey = ConfigurationManager.AppSettings["wrongKeyFileName"];
      if (String.IsNullOrEmpty(password)) {
        throw new ArgumentNullException("wrongKeyFileName");
      }
      _wrongKey = File.ReadAllBytes(wrongKey);

      _manager = ORBInitializer.Manager;
    }

    private static Connection CreateConnection() {
      Connection conn = _manager.CreateConnection(_hostName, _hostPort);
      return conn;
    }

    /// <summary>
    /// Teste da auto-propriedade ORB.
    ///</summary>
    [TestMethod]
    public void ORBTest() {
      lock (this) {
        Connection conn = CreateConnection();
        Assert.IsNotNull(conn.ORB);
        Assert.AreEqual(conn.ORB, _manager.ORB);
      }
    }

    /// <summary>
    /// Teste da auto-propriedade OfferRegistry
    ///</summary>
    [TestMethod]
    public void OfferRegistryTest() {
      lock (this) {
        Connection conn = CreateConnection();
        try {
          conn.Offers.findServices(new[] {new ServiceProperty("a", "b")});
        }
        catch (NO_PERMISSION e) {
          Assert.AreEqual(e.Minor, NoLoginCode.ConstVal);
        }
        catch (Exception e) {
          Assert.Fail(e.Message);
        }
      }
    }

    /// <summary>
    /// Teste da auto-propriedade BusId
    ///</summary>
    [TestMethod]
    public void BusIdTest() {
      lock (this) {
        Connection conn = CreateConnection();
        Assert.IsNull(conn.BusId);
        conn.LoginByPassword(_login, _password);
        Assert.IsNotNull(conn.BusId);
        Assert.IsTrue(conn.Logout());
        Assert.IsNull(conn.Login);
      }
    }

    /// <summary>
    /// Teste da auto-propriedade Login
    ///</summary>
    [TestMethod]
    public void LoginTest() {
      lock (this) {
        Connection conn = CreateConnection();
        Assert.IsNull(conn.Login);
        conn.LoginByPassword(_login, _password);
        Assert.IsNotNull(conn.Login);
        conn.Logout();
        Assert.IsNull(conn.Login);
      }
    }

    /// <summary>
    /// Testes do método LoginByPassword
    ///</summary>
    [TestMethod]
    public void LoginByPasswordTest() {
      lock (this) {
        Connection conn = CreateConnection();
        // entidade errada
        bool failed = false;
        try {
          conn.LoginByPassword("", _password);
        }
        catch (AccessDenied) {
          failed = true;
        }
        catch (Exception e) {
          Assert.Fail("A exceção deveria ser AccessDenied. Exceção recebida: " +
                      e);
        }
        Assert.IsTrue(failed, "O login com entidade vazia foi bem-sucedido.");
        // senha errada
        failed = false;
        try {
          conn.LoginByPassword(_login, new byte[0]);
        }
        catch (AccessDenied) {
          failed = true;
        }
        catch (Exception e) {
          Assert.Fail("A exceção deveria ser AccessDenied. Exceção recebida: " +
                      e);
        }
        Assert.IsTrue(failed, "O login com senha vazia foi bem-sucedido.");
        // login válido
        Assert.IsNull(conn.Login);
        conn.LoginByPassword(_login, _password);
        Assert.IsNotNull(conn.Login);
        conn.Logout();
        Assert.IsNull(conn.Login);
        // login repetido
        failed = false;
        try {
          conn.LoginByPassword(_login, _password);
          conn.LoginByPassword(_login, _password);
        }
        catch (AlreadyLoggedInException) {
          failed = true;
        }
        catch (Exception e) {
          Assert.Fail(
            "A exceção deveria ser AlreadyLoggedInException. Exceção recebida: " +
            e);
        }
        Assert.IsTrue(failed,
                      "O login com entidade já autenticada foi bem-sucedido.");
        conn.Logout();
        Assert.IsNull(conn.Login);
      }
    }

    /// <summary>
    /// Testes do método LoginByCertificate
    ///</summary>
    [TestMethod]
    public void LoginByCertificateTest() {
      lock (this) {
        Connection conn = CreateConnection();
        // entidade sem certificado cadastrado
        bool failed = false;
        try {
          conn.LoginByCertificate(_entityNoCert, _privKey);
        }
        catch (MissingCertificate) {
          failed = true;
        }
        catch (Exception e) {
          Assert.Fail(
            "A exceção deveria ser MissingCertificate. Exceção recebida: " + e);
        }
        Assert.IsTrue(failed,
                      "O login de entidade sem certificado cadastrado foi bem-sucedido.");
        // chave privada corrompida
        failed = false;
        try {
          conn.LoginByCertificate(_entity, new byte[0]);
        }
        catch (CorruptedPrivateKeyException) {
          failed = true;
        }
        catch (Exception e) {
          Assert.Fail(
            "A exceção deveria ser CorruptedPrivateKeyException. Exceção recebida: " +
            e);
        }
        Assert.IsTrue(failed,
                      "O login de entidade com chave corrompida foi bem-sucedido.");
        // chave privada inválida
        failed = false;
        try {
          conn.LoginByCertificate(_entity, _wrongKey);
        }
        catch (WrongPrivateKeyException) {
          failed = true;
        }
        catch (Exception e) {
          Assert.Fail(
            "A exceção deveria ser WrongPrivateKeyException. Exceção recebida: " +
            e);
        }
        Assert.IsTrue(failed,
                      "O login de entidade com chave errada foi bem-sucedido.");
        // login válido
        Assert.IsNull(conn.Login);
        conn.LoginByCertificate(_entity, _privKey);
        Assert.IsNotNull(conn.Login);
        Assert.IsTrue(conn.Logout());
        Assert.IsNull(conn.Login);
        // login repetido
        failed = false;
        try {
          conn.LoginByCertificate(_entity, _privKey);
          conn.LoginByCertificate(_entity, _privKey);
        }
        catch (AlreadyLoggedInException) {
          failed = true;
        }
        catch (Exception e) {
          Assert.Fail(
            "A exceção deveria ser AlreadyLoggedInException. Exceção recebida: " +
            e);
        }
        Assert.IsTrue(failed,
                      "O login com entidade já autenticada foi bem-sucedido.");
        Assert.IsTrue(conn.Logout());
        Assert.IsNull(conn.Login);
      }
    }

    /// <summary>
    /// Testes de SingleSignOn
    ///</summary>
    [TestMethod]
    public void SingleSignOnTest() {
      lock (this) {
        Connection conn = CreateConnection();
        Connection conn2 = CreateConnection();
        conn.LoginByPassword(_login, _password);
        // segredo errado
        bool failed = false;
        byte[] secret;
        LoginProcess login;
        try {
          _manager.Requester = conn;
          login = conn.StartSingleSignOn(out secret);
          _manager.Requester = null;
          conn2.LoginBySingleSignOn(login, new byte[0]);
        }
        catch (WrongSecretException) {
          failed = true;
        }
        catch (Exception e) {
          Assert.Fail(
            "A exceção deveria ser WrongSecretException. Exceção recebida: " + e);
        }
        Assert.IsTrue(failed, "O login com segredo errado foi bem-sucedido.");
        // login válido
        Assert.IsNull(conn2.Login);
        _manager.Requester = conn;
        login = conn.StartSingleSignOn(out secret);
        _manager.Requester = null;
        conn2.LoginBySingleSignOn(login, secret);
        Assert.IsNotNull(conn2.Login);
        conn2.Logout();
        Assert.IsNull(conn2.Login);
        _manager.Requester = null;
        // login repetido
        failed = false;
        try {
          _manager.Requester = conn;
          login = conn.StartSingleSignOn(out secret);
          _manager.Requester = null;
          conn2.LoginBySingleSignOn(login, secret);
          Assert.IsNotNull(conn2.Login);
          conn2.LoginBySingleSignOn(login, secret);
        }
        catch (AlreadyLoggedInException) {
          failed = true;
        }
        catch (Exception e) {
          Assert.Fail(
            "A exceção deveria ser AlreadyLoggedInException. Exceção recebida: " +
            e);
        }
        Assert.IsTrue(failed,
                      "O login com entidade já autenticada foi bem-sucedido.");
        conn2.Logout();
        Assert.IsNull(conn2.Login);
        conn.Logout();
        Assert.IsNull(conn.Login);
        _manager.Requester = null;
      }
    }

    /// <summary>
    /// Testes do método Logout
    ///</summary>
    [TestMethod]
    public void LogoutTest() {
      lock (this) {
        Connection conn = CreateConnection();
        Assert.IsFalse(conn.Logout());
        conn.LoginByPassword(_login, _password);
        string busId = conn.BusId;
        _manager.SetDispatcher(conn);
        Assert.AreEqual(_manager.GetDispatcher(busId), conn);
        Assert.IsTrue(conn.Logout());
        Assert.IsNull(_manager.GetDispatcher(busId));
        Assert.IsNull(conn.BusId);
        Assert.IsNull(conn.Login);
        bool failed = false;
        try {
          _manager.Requester = conn;
          conn.Offers.findServices(new ServiceProperty[0]);
        }
        catch (NO_PERMISSION e) {
          failed = true;
          if (e.Minor != NoLoginCode.ConstVal) {
            Assert.Fail(
              "A exceção é NO_PERMISSION mas o minor code não é NoLoginCode. Minor code recebido: " +
              e.Minor);
          }
        }
        catch (Exception e) {
          Assert.Fail(
            "A exceção deveria ser NO_PERMISSION. Exceção recebida: " +
            e);
        }
        finally {
          _manager.Requester = null;
        }
        Assert.IsTrue(failed, "Uma busca sem login foi bem-sucedida.");
      }
    }

    /// <summary>
    /// Testes da auto-propriedade OnInvalidLoginCallback
    ///</summary>
    [TestMethod]
    public void OnInvalidLoginCallbackTest() {
      lock (this) {
        Connection conn = CreateConnection();
        Assert.IsNull(conn.OnInvalidLogin);
        InvalidLoginCallback callback = new InvalidLoginCallbackMock();
        conn.OnInvalidLogin = callback;
        Assert.AreEqual(callback, conn.OnInvalidLogin);
      }
    }

    /// <summary>
    /// Testes da auto-propriedade CallerChain
    ///</summary>
    [TestMethod]
    public void CallerChainTest() {
      lock (this) {
        Connection conn = CreateConnection();
        Assert.IsNull(conn.CallerChain);
      }
      //TODO: adicionar testes para caso exista uma callerchain ou os testes de interoperabilidade ja cobrem isso de forma suficiente?
    }

    /// <summary>
    /// Testes do método JoinChain
    ///</summary>
    [TestMethod]
    public void JoinChainTest() {
      lock (this) {
        Connection conn = CreateConnection();
        Assert.IsNull(conn.JoinedChain);
        // adiciona a chain da getCallerChain
        conn.JoinChain(null);
        Assert.IsNull(conn.JoinedChain);
        //TODO testar caso em que a chain da getCallerChain não é vazia
        conn.JoinChain(new CallerChainImpl("mock",
                                           new[] {new LoginInfo("a", "b")}));
        Assert.IsNotNull(conn.JoinedChain);
        Assert.AreEqual("mock", conn.JoinedChain.BusId);
        Assert.AreEqual("a", conn.JoinedChain.Callers[0].id);
        Assert.AreEqual("b", conn.JoinedChain.Callers[0].entity);
        conn.ExitChain();
      }
    }

    /// <summary>
    /// Testes do método ExitChain
    ///</summary>
    [TestMethod]
    public void ExitChainTest() {
      lock (this) {
        Connection conn = CreateConnection();
        Assert.IsNull(conn.JoinedChain);
        conn.ExitChain();
        Assert.IsNull(conn.JoinedChain);
        conn.JoinChain(new CallerChainImpl("mock",
                                           new[] {new LoginInfo("a", "b")}));
        conn.ExitChain();
        Assert.IsNull(conn.JoinedChain);
      }
    }
  }
}