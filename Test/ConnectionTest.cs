using System.Configuration;
using System.IO;
using System.Runtime.CompilerServices;
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

    private Connection CreateConnection() {
      Connection conn = _manager.CreateConnection(_hostName, _hostPort);
      _manager.DefaultConnection = conn;
      return conn;
    }

    /// <summary>
    /// Teste da auto-propriedade ORB.
    ///</summary>
    [TestMethod]
    [MethodImpl(MethodImplOptions.Synchronized)]
    public void ORBTest() {
      Connection conn = CreateConnection();
      Assert.IsNotNull(conn.ORB);
      Assert.AreEqual(conn.ORB, _manager.ORB);
    }

    /// <summary>
    /// Teste da auto-propriedade OfferRegistry
    ///</summary>
    [TestMethod]
    [MethodImpl(MethodImplOptions.Synchronized)]
    public void OfferRegistryTest() {
      Connection conn = CreateConnection();
      try {
        conn.OfferRegistry.findServices(new[] {new ServiceProperty("a", "b")});
      }
      catch (NO_PERMISSION) {
      }
      catch (Exception e) {
        Assert.Fail(e.Message);
      }
    }

    /// <summary>
    /// Teste da auto-propriedade BusId
    ///</summary>
    [TestMethod]
    [MethodImpl(MethodImplOptions.Synchronized)]
    public void BusIdTest() {
      Connection conn = CreateConnection();
      Assert.IsNotNull(conn.BusId);
    }

    /// <summary>
    /// Teste da auto-propriedade Login
    ///</summary>
    [TestMethod]
    [MethodImpl(MethodImplOptions.Synchronized)]
    public void LoginTest() {
      Connection conn = CreateConnection();
      Assert.IsNull(conn.Login);
      conn.LoginByPassword(_login, _password);
      Assert.IsNotNull(conn.Login);
      conn.Logout();
      Assert.IsNull(conn.Login);
    }

    /// <summary>
    /// Testes do método LoginByPassword
    ///</summary>
    [TestMethod]
    [MethodImpl(MethodImplOptions.Synchronized)]
    public void LoginByPasswordTest() {
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
        Assert.Fail("A exceção deveria ser AccessDenied. Exceção recebida: " + e);
      }
      if (!failed) {
        Assert.Fail("O login com entidade vazia foi bem-sucedido.");
      }
      // senha errada
      failed = false;
      try {
        conn.LoginByPassword(_login, new byte[0]);
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
      if (!failed) {
        Assert.Fail("O login com entidade já autenticada foi bem-sucedido.");
      }
      conn.Logout();
    }

    /// <summary>
    /// Testes do método LoginByCertificate
    ///</summary>
    [TestMethod]
    [MethodImpl(MethodImplOptions.Synchronized)]
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
        Assert.Fail(
          "A exceção deveria ser MissingCertificate. Exceção recebida: " + e);
      }
      if (!failed) {
        Assert.Fail(
          "O login de entidade sem certificado cadastrado foi bem-sucedido.");
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
        Assert.Fail(
          "A exceção deveria ser CorruptedPrivateKeyException. Exceção recebida: " +
          e);
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
        Assert.Fail(
          "A exceção deveria ser WrongPrivateKeyException. Exceção recebida: " +
          e);
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
        Assert.Fail(
          "A exceção deveria ser AlreadyLoggedInException. Exceção recebida: " +
          e);
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
    [MethodImpl(MethodImplOptions.Synchronized)]
    public void SingleSignOnTest() {
      //TODO teste de singlesignon e de loginbycertificate ainda estao com erros.
      //TODO remover linhas [MethodImpl(MethodImplOptions.Synchronized)] pois nao resolveu o problema de concorrencia do MSTests. Tentar sincronizar na mão.
      Connection conn = CreateConnection();
      Connection conn2 = CreateConnection();
      conn.LoginByPassword(_login, _password);
      // segredo errado
      bool failed = false;
      byte[] secret;
      LoginProcess login;
      try {
        login = conn.StartSingleSignOn(out secret);
        conn2.LoginBySingleSignOn(login, new byte[0]);
      }
      catch (WrongSecretException) {
        failed = true;
      }
      catch (Exception e) {
        Assert.Fail(
          "A exceção deveria ser WrongSecretException. Exceção recebida: " + e);
      }
      if (!failed) {
        Assert.Fail("O login com segredo errado foi bem-sucedido.");
      }
      // login válido
      Assert.IsNull(conn2.Login);
      login = conn.StartSingleSignOn(out secret);
      conn2.LoginBySingleSignOn(login, secret);
      Assert.IsNotNull(conn2.Login);
      conn2.Logout();
      Assert.IsNull(conn2.Login);
      // login repetido
      failed = false;
      try {
        login = conn.StartSingleSignOn(out secret);
        conn2.LoginBySingleSignOn(login, secret);
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
      if (!failed) {
        Assert.Fail("O login com entidade já autenticada foi bem-sucedido.");
      }
      conn2.Logout();
      conn.Logout();
    }

    /// <summary>
    /// Testes do método Logout
    ///</summary>
    [TestMethod]
    [MethodImpl(MethodImplOptions.Synchronized)]
    public void LogoutTest() {
      Connection conn = CreateConnection();
      Assert.IsFalse(conn.Logout());
      conn.LoginByPassword(_login, _password);
      Assert.IsTrue(conn.Logout());
      Assert.IsNull(conn.Login);
      bool failed = false;
      try {
        conn.OfferRegistry.findServices(new ServiceProperty[0]);
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
        Assert.Fail("A exceção deveria ser NO_PERMISSION. Exceção recebida: " +
                    e);
      }
      if (!failed) {
        Assert.Fail("Uma busca sem login foi bem-sucedida.");
      }
    }

    /// <summary>
    /// Testes da auto-propriedade OnInvalidLoginCallback
    ///</summary>
    [TestMethod]
    [MethodImpl(MethodImplOptions.Synchronized)]
    public void OnInvalidLoginCallbackTest() {
      Connection conn = CreateConnection();
      Assert.IsNull(conn.OnInvalidLoginCallback);
      InvalidLoginCallback callback = new InvalidLoginCallbackMock();
      conn.OnInvalidLoginCallback = callback;
      Assert.AreEqual(callback, conn.OnInvalidLoginCallback);
    }

    /// <summary>
    /// Testes da auto-propriedade CallerChain
    ///</summary>
    [TestMethod]
    [MethodImpl(MethodImplOptions.Synchronized)]
    public void CallerChainTest() {
      Connection conn = CreateConnection();
      Assert.IsNull(conn.CallerChain);
      //TODO: adicionar testes para caso exista uma callerchain ou os testes de interoperabilidade ja cobrem isso de forma suficiente?
    }

    /// <summary>
    /// Testes do método JoinChain
    ///</summary>
    [TestMethod]
    [MethodImpl(MethodImplOptions.Synchronized)]
    public void JoinChainTest() {
      Connection conn = CreateConnection();
      Assert.IsNull(conn.JoinedChain);
      // adiciona a chain da getCallerChain
      conn.JoinChain(null);
      Assert.IsNull(conn.JoinedChain);
      //TODO testar caso em que a chain da getCallerChain não é vazia
      conn.JoinChain(new CallerChainImpl("mock", new []{new LoginInfo("a", "b")}));
      Assert.IsNotNull(conn.JoinedChain);
      Assert.AreEqual("mock", conn.JoinedChain.BusId);
      Assert.AreEqual("a", conn.JoinedChain.Callers[0].id);
      Assert.AreEqual("b", conn.JoinedChain.Callers[0].entity);
      conn.ExitChain();
    }

    /// <summary>
    /// Testes do método ExitChain
    ///</summary>
    [TestMethod]
    [MethodImpl(MethodImplOptions.Synchronized)]
    public void ExitChainTest() {
      Connection conn = CreateConnection();
      Assert.IsNull(conn.JoinedChain);
      conn.ExitChain();
      Assert.IsNull(conn.JoinedChain);
      conn.JoinChain(new CallerChainImpl("mock", new[] { new LoginInfo("a", "b") }));
      conn.ExitChain();
      Assert.IsNull(conn.JoinedChain);
    }
  }
}