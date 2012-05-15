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

    protected virtual Connection CreateConnection() {
      Connection conn = _manager.CreateConnection(_hostName, _hostPort);
      _manager.DefaultConnection = conn;
      return conn;
    }

    /// <summary>
    /// Teste da auto-propriedade ORB.
    ///</summary>
    [TestMethod]
    public void ORBTest() {
      //TODO: implementar ou remover
      Assert.Inconclusive("Esse teste deve existir mesmo?");
    }

    /// <summary>
    /// Teste da auto-propriedade OfferRegistry
    ///</summary>
    [TestMethod]
    public void OfferRegistryTest() {
      Connection conn = CreateConnection();
      try {
        conn.OfferRegistry.findServices(new[] { new ServiceProperty("a", "b") });
      }
      catch (NO_PERMISSION) {
      }
      catch(Exception e) {
        Assert.Fail(e.Message);
      }
    }

    /// <summary>
    /// Teste da auto-propriedade BusId
    ///</summary>
    [TestMethod]
    public void BusIdTest() {
      Connection conn = CreateConnection();
      Assert.IsNotNull(conn.BusId);
    }

    /// <summary>
    /// Teste da auto-propriedade Login
    ///</summary>
    [TestMethod]
    public void LoginTest() {
      Connection conn = CreateConnection();
      Assert.IsNull(conn.Login);
      conn.LoginByPassword(_entity, _password);
      Assert.IsNotNull(conn.Login);
      conn.Logout();
      Assert.IsNull(conn.Login);
    }

    /// <summary>
    /// Testes do método LoginByPassword
    ///</summary>
    [TestMethod]
    public void LoginByPasswordTest() {
      Connection conn = CreateConnection();
      // entidade errada
      bool failed = false;
      try {
        conn.LoginByPassword("", _password);
      }
      catch(AccessDenied) {
        failed = true;
      }
      catch (Exception e) {
        Assert.Fail("A exceção deveria ser AccessDenied. Exceção recebida: " + e);
      }
      if (!failed) {
        Assert.Fail("O login com entidade vazia foi bem-sucedido.");
      }
      // senha errada
      failed = false;
      try {
        conn.LoginByPassword(_entity, new byte[0]);
      }
      catch (AccessDenied) {
        failed = true;
      }
      catch (Exception e) {
        Assert.Fail("A exceção deveria ser AccessDenied. Exceção recebida: " + e);
      }
      if (!failed) {
        Assert.Fail("O login com senha vazia foi bem-sucedido.");
      }
      // login válido
      Assert.IsNull(conn.Login);
      conn.LoginByPassword(_entity, _password);
      Assert.IsNotNull(conn.Login);
      conn.Logout();
      Assert.IsNull(conn.Login);
      // login repetido
      failed = false;
      try {
        conn.LoginByPassword(_entity, _password);
        conn.LoginByPassword(_entity, _password);
      }
      catch (AlreadyLoggedInException) {
        failed = true;
      }
      catch (Exception e) {
        Assert.Fail("A exceção deveria ser AlreadyLoggedInException. Exceção recebida: " + e);
      }
      if (!failed) {
        Assert.Fail("O login com entidade já autenticada foi bem-sucedido.");
      }
      conn.Logout();
    }

    /// <summary>
    /// Testes do método LoginByCertificate
    ///</summary>
    [TestMethod]
    public void LoginByCertificateTest() {
      Connection conn = CreateConnection();
      // entidade errada
      bool failed = false;
      try {
        conn.LoginByCertificate("", _privKey);
      }
      catch (AccessDenied) {
        failed = true;
      }
      catch (Exception e) {
        Assert.Fail("A exceção deveria ser AccessDenied. Exceção recebida: " + e);
      }
      if (!failed) {
        Assert.Fail("O login com entidade vazia foi bem-sucedido.");
      }
      // entidade sem certificado cadastrado
      failed = false;
      try {
        conn.LoginByCertificate(_entityNoCert, _privKey);
      }
      catch (MissingCertificate) {
        failed = true;
      }
      catch (Exception e) {
        Assert.Fail("A exceção deveria ser MissingCertificate. Exceção recebida: " + e);
      }
      if (!failed) {
        Assert.Fail("O login de entidade sem certificado cadastrado foi bem-sucedido.");
      }
      // chave privada corrompida
      failed = false;
      try {
        conn.LoginByCertificate(_entity, new byte[0]);
      }
      catch (CorruptedPrivateKeyException) {
        failed = true;
      }
      catch (Exception e) {
        Assert.Fail("A exceção deveria ser CorruptedPrivateKeyException. Exceção recebida: " + e);
      }
      if (!failed) {
        Assert.Fail("O login de entidade com chave corrompida foi bem-sucedido.");
      }
      // chave privada inválida
      failed = false;
      try {
        conn.LoginByCertificate(_entity, _wrongKey);
      }
      catch (WrongPrivateKeyException) {
        failed = true;
      }
      catch (Exception e) {
        Assert.Fail("A exceção deveria ser WrongPrivateKeyException. Exceção recebida: " + e);
      }
      if (!failed) {
        Assert.Fail("O login de entidade com chave errada foi bem-sucedido.");
      }
      // login válido
      Assert.IsNull(conn.Login);
      conn.LoginByCertificate(_entity, _privKey);
      Assert.IsNotNull(conn.Login);
      conn.Logout();
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
        Assert.Fail("A exceção deveria ser AlreadyLoggedInException. Exceção recebida: " + e);
      }
      if (!failed) {
        Assert.Fail("O login com entidade já autenticada foi bem-sucedido.");
      }
      conn.Logout();
    }

    /// <summary>
    /// Testes de SingleSignOn
    ///</summary>
    [TestMethod]
    public void SingleSignOnTest() {
      Connection conn = CreateConnection();
      // segredo errado
      bool failed = false;
      byte[] secret;
      LoginProcess login;
      try {
        login = conn.StartSingleSignOn(out secret);
        conn.LoginBySingleSignOn(login, new byte[0]);
      }
      catch (WrongSecretException) {
        failed = true;
      }
      catch (Exception e) {
        Assert.Fail("A exceção deveria ser WrongSecretException. Exceção recebida: " + e);
      }
      if (!failed) {
        Assert.Fail("O login com segredo errado foi bem-sucedido.");
      }
      // login válido
      Assert.IsNull(conn.Login);
      login = conn.StartSingleSignOn(out secret);
      conn.LoginBySingleSignOn(login, secret);
      Assert.IsNotNull(conn.Login);
      conn.Logout();
      Assert.IsNull(conn.Login);
      // login repetido
      failed = false;
      try {
        login = conn.StartSingleSignOn(out secret);
        conn.LoginBySingleSignOn(login, secret);
        conn.LoginBySingleSignOn(login, secret);
      }
      catch (AlreadyLoggedInException) {
        failed = true;
      }
      catch (Exception e) {
        Assert.Fail("A exceção deveria ser AlreadyLoggedInException. Exceção recebida: " + e);
      }
      if (!failed) {
        Assert.Fail("O login com entidade já autenticada foi bem-sucedido.");
      }
      conn.Logout();
    }

    /// <summary>
    /// Testes do método Logout
    ///</summary>
    [TestMethod]
    public void LogoutTest() {
      Connection target = CreateConnection();
      // TODO: Initialize to an appropriate value
      bool expected = false; // TODO: Initialize to an appropriate value
      bool actual;
      actual = target.Logout();
      Assert.AreEqual(expected, actual);
      Assert.Inconclusive("Verify the correctness of this test method.");
    }

    /// <summary>
    /// Testes do método ExitChain
    ///</summary>
    [TestMethod]
    public void ExitChainTest() {
      Connection target = CreateConnection();
      // TODO: Initialize to an appropriate value
      target.ExitChain();
      Assert.Inconclusive(
        "A method that does not return a value cannot be verified.");
    }

    /// <summary>
    /// Testes do método JoinChain
    ///</summary>
    [TestMethod]
    public void JoinChainTest() {
      Connection target = CreateConnection();
      // TODO: Initialize to an appropriate value
      CallerChain chain = null; // TODO: Initialize to an appropriate value
      target.JoinChain(chain);
      Assert.Inconclusive(
        "A method that does not return a value cannot be verified.");
    }

    /// <summary>
    /// Testes do método CallerChain
    ///</summary>
    [TestMethod]
    public void CallerChainTest() {
      Connection target = CreateConnection();
      // TODO: Initialize to an appropriate value
      CallerChain actual;
      actual = target.CallerChain;
      Assert.Inconclusive("Verify the correctness of this test method.");
    }

    /// <summary>
    /// Testes do método JoinedChain
    ///</summary>
    [TestMethod]
    public void JoinedChainTest() {
      Connection target = CreateConnection();
      // TODO: Initialize to an appropriate value
      CallerChain actual;
      actual = target.JoinedChain;
      Assert.Inconclusive("Verify the correctness of this test method.");
    }

    /// <summary>
    /// Testes do método OnInvalidLoginCallback
    ///</summary>
    [TestMethod]
    public void OnInvalidLoginCallbackTest() {
      Connection target = CreateConnection();
      // TODO: Initialize to an appropriate value
      InvalidLoginCallback expected = null;
      // TODO: Initialize to an appropriate value
      InvalidLoginCallback actual;
      target.OnInvalidLoginCallback = expected;
      actual = target.OnInvalidLoginCallback;
      Assert.AreEqual(expected, actual);
      Assert.Inconclusive("Verify the correctness of this test method.");
    }
  }
}