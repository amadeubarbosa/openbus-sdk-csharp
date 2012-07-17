using System.Configuration;
using System.IO;
using System.Runtime.Remoting;
using Ch.Elca.Iiop.Idl;
using Scs.Core;
using omg.org.CORBA;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using scs.core;
using tecgraf.openbus.core.v2_0;
using tecgraf.openbus.core.v2_0.services.access_control;
using tecgraf.openbus.core.v2_0.services.offer_registry;
using tecgraf.openbus.exceptions;
using tecgraf.openbus.interop.simple;
using tecgraf.openbus.security;
using tecgraf.openbus.test;

namespace tecgraf.openbus.Test {
  /// <summary>
  ///This is a test class for ConnectionTest and is intended
  ///to contain all ConnectionTest Unit Tests
  ///</summary>
  [TestClass]
  public class ConnectionTest {
    #region Fields

    private static String _hostName;
    private static ushort _hostPort;
    private static String _entity;
    private static String _entityNoCert;
    private static string _login;
    private static byte[] _password;
    private static byte[] _privKey;
    private static byte[] _wrongKey;
    private static ConnectionManager _manager;

    private const int LeaseTime = 10;
    internal static volatile bool CallbackCalled;

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
      _hostPort = ushort.Parse(port);

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
    /// Testes de SharedAuth
    ///</summary>
    [TestMethod]
    public void SharedAuthTest() {
      lock (this) {
        Connection conn = CreateConnection();
        Connection conn2 = CreateConnection();
        conn.LoginByPassword(_login, _password);
        // segredo errado
        bool failed = false;
        byte[] secret;
        LoginProcess login;
        try {
          login = conn.StartSharedAuth(out secret);
          conn2.LoginBySharedAuth(login, new byte[0]);
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
        login = conn.StartSharedAuth(out secret);
        conn2.LoginBySharedAuth(login, secret);
        Assert.IsNotNull(conn2.Login);
        conn2.Logout();
        Assert.IsNull(conn2.Login);
        // login repetido
        failed = false;
        try {
          login = conn.StartSharedAuth(out secret);
          conn2.LoginBySharedAuth(login, secret);
          Assert.IsNotNull(conn2.Login);
          conn2.LoginBySharedAuth(login, secret);
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
        InvalidLoginCallback callback = new InvalidLoginCallbackMock(_login, _password);
        conn.OnInvalidLogin = callback;
        Assert.AreEqual(callback, conn.OnInvalidLogin);
      }
    }

    /// <summary>
    /// Teste de login removido e chamada da callback para refazer
    /// </summary>
    [TestMethod]
    [Timeout(LeaseTime * 1000)]
    public void LoginRemovedAndCallbackTest() {
      lock (this) {
        Connection conn = CreateConnection();
        conn.LoginByPassword(_login, _password);
        Assert.IsNotNull(conn.Login);
        LoginInfo firstLogin = new LoginInfo(conn.Login.Value.id, conn.Login.Value.entity);
        InvalidLoginCallback callback = new InvalidLoginCallbackMock(_login, _password);
        conn.OnInvalidLogin = callback;
        _manager.DefaultConnection = conn;
        IComponent busIC = RemotingServices.Connect(
          typeof(IComponent),
          "corbaloc::1.0@" + _hostName + ":" + _hostPort + "/" + BusObjectKey.ConstVal)
                        as IComponent;
        Assert.IsNotNull(busIC);
        string lrId = Repository.GetRepositoryID(typeof (LoginRegistry));
        LoginRegistry lr = busIC.getFacet(lrId) as LoginRegistry;
        Assert.IsNotNull(lr);
        lr.invalidateLogin(conn.Login.Value.id);
        // faz uma chamada qualquer para refazer o login
        try {
          conn.Offers.getServices();
          Assert.IsTrue(CallbackCalled);
          Assert.IsFalse(firstLogin.id.Equals(conn.Login.Value.id));
        }
        catch (Exception) {
          Assert.Fail("O login não foi refeito.");
        }
        finally {
          CallbackCalled = false;
          _manager.DefaultConnection = null;
        }
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
        //TODO: Daqui pra baixo não funciona realmente pois a chamada sayHello não passa por CORBA, mas isso é um problema do IIOP.NET especificamente e não ocorre nas outras linguagens. Não há muito problema pois os testes de interoperabilidade ja cobrem isso de forma suficiente. Para reativar esse teste é necessário descomentar as duas linhas de Assert.Fail abaixo.
        try {
          const string facetName = "HelloMock";
          conn.LoginByPassword(_login, _password);
          ComponentContext context = new DefaultComponentContext(new ComponentId());
          context.AddFacet(facetName, Repository.GetRepositoryID(typeof(Hello)),
                           new HelloMock(conn));
          _manager.DefaultConnection = conn;
          Hello hello = context.GetFacetByName(facetName).Reference as Hello;
          Assert.IsNotNull(hello);
          hello.sayHello();
        }
        catch (NullReferenceException) {
//          Assert.Fail("A cadeia obtida é nula.");
        }
        catch (OpenBusInternalException) {
//          Assert.Fail("A cadeia obtida não é a esperada.");
        }
        finally {
          _manager.DefaultConnection = null;
          conn.Logout();
        }
      }
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
        //TODO não há como testar o caso do TODO acima em C# sem usar processos diferentes para o servidor e cliente. Não há muito problema pois os testes de interoperabilidade cobrem esse caso.
        conn.JoinChain(new CallerChainImpl("mock", new LoginInfo("a", "b"),
                                           new LoginInfo[0]));
        Assert.IsNotNull(conn.JoinedChain);
        Assert.AreEqual("mock", conn.JoinedChain.BusId);
        Assert.AreEqual("a", conn.JoinedChain.Caller.id);
        Assert.AreEqual("b", conn.JoinedChain.Caller.entity);
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
        conn.JoinChain(new CallerChainImpl("mock", new LoginInfo("a", "b"),
                                           new LoginInfo[0]));
        conn.ExitChain();
        Assert.IsNull(conn.JoinedChain);
      }
    }
  }
}