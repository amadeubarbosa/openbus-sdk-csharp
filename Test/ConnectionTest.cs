using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Ch.Elca.Iiop.Idl;
using omg.org.CORBA;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.IO;
using Ch.Elca.Iiop;
using Ch.Elca.Iiop.Security.Ssl;
using Org.BouncyCastle.Crypto;
using scs.core;
using tecgraf.openbus.caches;
using tecgraf.openbus.core.v2_1;
using tecgraf.openbus.core.v2_1.services.access_control;
using tecgraf.openbus.core.v2_1.services.offer_registry;
using tecgraf.openbus.exceptions;
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
    private static string _busIOR;
    private static MarshalByRefObject _busRef;
    private static String _entity;
    private static String _entityNoCert;
    private static string _login;
    private static byte[] _password;
    private static string _domain;
    private static AsymmetricCipherKeyPair _privKey;
    private static AsymmetricCipherKeyPair _wrongKey;
    private static bool _useSSL;
    private static OpenBusContext _context;
    private static readonly ConnectionProperties Props = new ConnectionPropertiesImpl();

    private const int LeaseTime = 10;
    internal static volatile bool CallbackCalled;

    /// <summary>
    ///Gets or sets the test context which provides
    ///information about and functionality for the current test run.
    ///</summary>
    public TestContext TestContext { get; set; }

    #endregion

    #region Additional test attributes

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
    //Use TestCleanup to run code after each test has run
    //[TestCleanup]
    //public void MyTestCleanup() {
    //}


    #endregion

    //Use ClassInitialize to run code before running the first test in the class
    [ClassInitialize]
    public static void MyClassInitialize(TestContext testContext) {
      _hostName = ConfigurationManager.AppSettings["hostName"];

      string port = ConfigurationManager.AppSettings["hostPort"];
      if (!String.IsNullOrEmpty(port)) {
        _hostPort = ushort.Parse(port);
      }

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
      _privKey = Crypto.ReadKeyFile(privateKey);
      Props.AccessKey = _privKey;

      string wrongKey = ConfigurationManager.AppSettings["wrongKeyFileName"];
      if (String.IsNullOrEmpty(password)) {
        throw new ArgumentNullException("wrongKeyFileName");
      }
      _wrongKey = Crypto.ReadKeyFile(wrongKey);

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
        string[] iors = File.ReadAllLines(_busIOR);
        _busIOR = iors[0];
        _busRef = (MarshalByRefObject)OrbServices.CreateProxy(typeof(MarshalByRefObject), _busIOR);
      }
      else {
        ORBInitializer.InitORB();
      }
      _context = ORBInitializer.Context;
    }

    /// <summary>
    /// Teste da auto-propriedade ORB.
    ///</summary>
    [TestMethod]
    public void ORBTest() {
      lock (this) {
        Connection conn = ConnectToBus();
        Assert.IsNotNull(conn.ORB);
        Assert.AreEqual(conn.ORB, _context.ORB);
      }
    }

    /// <summary>
    /// Teste da auto-propriedade OfferRegistry
    ///</summary>
    [TestMethod]
    public void OfferRegistryTest() {
      lock (this) {
        Connection conn = ConnectToBus();
        _context.SetCurrentConnection(conn);
        try {
          _context.OfferRegistry.findServices(new[] {new ServiceProperty("a", "b")});
        }
        catch (NO_PERMISSION e) {
          Assert.AreEqual(e.Minor, NoLoginCode.ConstVal);
        }
        catch (Exception e) {
          Assert.Fail(e.Message);
        }
        finally {
          _context.SetCurrentConnection(null);
        }
      }
    }

    /// <summary>
    /// Teste da auto-propriedade BusId
    ///</summary>
    [TestMethod]
    public void BusIdTest() {
      lock (this) {
        Connection conn = ConnectToBus();
        Assert.IsNull(conn.BusId);
        conn.LoginByPassword(_login, _password, _domain);
        Assert.IsNotNull(conn.BusId);
        Assert.IsTrue(conn.Logout());
        Assert.IsNull(conn.BusId);
      }
    }

    /// <summary>
    /// Teste da auto-propriedade Login
    ///</summary>
    [TestMethod]
    public void LoginTest() {
      lock (this) {
        Connection conn = ConnectToBus();
        Assert.IsNull(conn.Login);
        conn.LoginByPassword(_login, _password, _domain);
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
        Connection conn = ConnectToBus();
        bool failed = false;
        // login nulo
        try {
          conn.LoginByPassword(null, _password, _domain);
        }
        catch (ArgumentException) {
          failed = true;
        }
        catch (Exception e) {
          Assert.Fail("A exceção deveria ser ArgumentException. Exceção recebida: " +
                      e);
        }
        Assert.IsTrue(failed, "O login com entidade nula foi bem-sucedido.");
        // senha nula
        failed = false;
        try {
          conn.LoginByPassword(_login, null, _domain);
        }
        catch (ArgumentException) {
          failed = true;
        }
        catch (Exception e) {
          Assert.Fail("A exceção deveria ser ArgumentException. Exceção recebida: " +
                      e);
        }
        Assert.IsTrue(failed, "O login com senha nula foi bem-sucedido.");
        // entidade errada
        failed = false;
        try {
          conn.LoginByPassword("", _password, _domain);
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
          conn.LoginByPassword(_login, new byte[0], _domain);
        }
        catch (AccessDenied) {
          failed = true;
        }
        catch (Exception e) {
          Assert.Fail("A exceção deveria ser AccessDenied. Exceção recebida: " +
                      e);
        }
        Assert.IsTrue(failed, "O login com senha vazia foi bem-sucedido.");
        // domínio errado
        failed = false;
        try {
          conn.LoginByPassword(_login, _password, "UnknownDomain");
        }
        catch (UnknownDomain) {
          failed = true;
        }
        catch (Exception e) {
          Assert.Fail("A exceção deveria ser UnknownDomain. Exceção recebida: " +
                      e);
        }
        Assert.IsTrue(failed, "O login com domínio desconhecido foi bem-sucedido.");
        // login válido
        Assert.IsNull(conn.Login);
        conn.LoginByPassword(_login, _password, _domain);
        Assert.IsNotNull(conn.Login);
        conn.Logout();
        Assert.IsNull(conn.Login);
        // login repetido
        failed = false;
        try {
          conn.LoginByPassword(_login, _password, _domain);
          conn.LoginByPassword(_login, _password, _domain);
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
        Connection conn = ConnectToBus();
        bool failed = false;
        // login nulo
        try {
          conn.LoginByCertificate(null, _privKey);
        }
        catch (ArgumentException) {
          failed = true;
        }
        catch (Exception e) {
          Assert.Fail("A exceção deveria ser ArgumentException. Exceção recebida: " +
                      e);
        }
        Assert.IsTrue(failed, "O login com entidade nula foi bem-sucedido.");
        // chave privada nula
        failed = false;
        try {
          conn.LoginByCertificate(_login, null);
        }
        catch (ArgumentException) {
          failed = true;
        }
        catch (Exception e) {
          Assert.Fail("A exceção deveria ser ArgumentException. Exceção recebida: " +
                      e);
        }
        Assert.IsTrue(failed, "O login com chave privada nula foi bem-sucedido.");
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
        Assert.IsTrue(failed,
                      "O login de entidade sem certificado cadastrado foi bem-sucedido.");
        // chave privada corrompida
        failed = false;
        try {
          conn.LoginByCertificate(_entity, Crypto.ReadKey(new byte[0]));
        }
        catch (InvalidPrivateKeyException) {
          failed = true;
        }
        catch (Exception e) {
          Assert.Fail(
            "A exceção deveria ser CorruptedPrivateKeyException. Exceção recebida: " +
            e);
        }
        Assert.IsTrue(failed,
                      "O login de entidade com chave corrompida foi bem-sucedido.");
        // chave privada errada
        failed = false;
        try {
          conn.LoginByCertificate(_entity, _wrongKey);
        }
        catch (AccessDenied) {
          failed = true;
        }
        catch (Exception e) {
          Assert.Fail(
            "A exceção deveria ser AccessDenied. Exceção recebida: " +
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
        Connection conn = ConnectToBus();
        Connection conn2 = ConnectToBus();
        bool failed = false;
        SharedAuthSecretImpl secret;
        // sem login
        try {
          conn.StartSharedAuth();
        }
        catch (NO_PERMISSION e) {
          if (e.Minor.Equals(NoLoginCode.ConstVal)) {
            failed = true;
          }
          else {
            Assert.Fail(
              "A exceção deveria ser NO_PERMISSION{NoLogin}. Exceção recebida: " + e);
          }
        }
        catch (Exception e) {
          Assert.Fail(
            "A exceção deveria ser NO_PERMISSION{NoLogin}. Exceção recebida: " + e);
        }
        Assert.IsTrue(failed, "A autenticação compartilhada sem login foi bem sucedida.");
        failed = false;

        // segredo errado
        conn.LoginByPassword(_login, _password, _domain);
        try {
          secret = (SharedAuthSecretImpl) conn.StartSharedAuth();
          secret.Secret = new byte[0];
          conn2.LoginBySharedAuth(secret);
        }
        catch (AccessDenied) {
          failed = true;
        }
        catch (Exception e) {
          Assert.Fail(
            "A exceção deveria ser WrongSecretException. Exceção recebida: " + e);
        }
        Assert.IsTrue(failed, "O login com segredo errado foi bem-sucedido.");
        // login nulo
        failed = false;
        try {
          secret = (SharedAuthSecretImpl) conn.StartSharedAuth();
          secret.Attempt = null;
          conn2.LoginBySharedAuth(secret);
        }
        catch (ArgumentException) {
          failed = true;
        }
        catch (Exception e) {
          Assert.Fail(
            "A exceção deveria ser ArgumentException. Exceção recebida: " + e);
        }
        Assert.IsTrue(failed, "O login com LoginProcess nulo foi bem-sucedido.");
        // segredo nulo
        failed = false;
        try {
          secret = (SharedAuthSecretImpl) conn.StartSharedAuth();
          secret.Secret = null;
          conn2.LoginBySharedAuth(secret);
        }
        catch (ArgumentException) {
          failed = true;
        }
        catch (Exception e) {
          Assert.Fail(
            "A exceção deveria ser ArgumentException. Exceção recebida: " + e);
        }
        Assert.IsTrue(failed, "O login com segredo nulo foi bem-sucedido.");
        // login válido
        Assert.IsNull(conn2.Login);
        secret = (SharedAuthSecretImpl) conn.StartSharedAuth();
        conn2.LoginBySharedAuth(secret);
        Assert.IsNotNull(conn2.Login);
        conn2.Logout();
        Assert.IsNull(conn2.Login);
        // login repetido
        failed = false;
        try {
          secret = (SharedAuthSecretImpl) conn.StartSharedAuth();
          conn2.LoginBySharedAuth(secret);
          Assert.IsNotNull(conn2.Login);
          conn2.LoginBySharedAuth(secret);
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
        Connection conn = ConnectToBus();
        CallDispatchCallbackImpl dispatchCallback = new CallDispatchCallbackImpl(conn);
        Assert.IsFalse(conn.Logout());
        conn.LoginByPassword(_login, _password, _domain);
        _context.OnCallDispatch = dispatchCallback.Dispatch;
        Assert.IsTrue(conn.Logout());
        Assert.IsNull(conn.BusId);
        Assert.IsNull(conn.Login);
        Assert.IsFalse(conn.Logout());
        Assert.IsNull(conn.BusId);
        Assert.IsNull(conn.Login);
        _context.OnCallDispatch = null;
        bool failed = false;
        try {
          _context.SetCurrentConnection(conn);
          _context.OfferRegistry.findServices(new ServiceProperty[0]);
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
          _context.SetCurrentConnection(null);
        }
        Assert.IsTrue(failed, "Uma busca sem login foi bem-sucedida.");

        // testa se o logout usa a conexão correta (sem cadeia) pelo objeto no qual é chamado e não altera a conexão corrente nem a cadeia
        try {
          conn.LoginByPassword(_login, _password, _domain);
          Connection conn2 = ConnectToBus();
          _context.SetCurrentConnection(conn2);
          CallerChain dummyChain = new CallerChainImpl("", new LoginInfo(), "",
            new LoginInfo[0], ConnectionImpl.InvalidSignedData);
          _context.JoinChain(dummyChain);
          Assert.IsTrue(conn.Logout());
          Assert.IsNull(conn.BusId);
          Assert.IsNull(conn.Login);
          Assert.AreEqual(dummyChain, _context.JoinedChain);
          Assert.AreEqual(conn2, _context.GetCurrentConnection());
        }
        finally {
          _context.ExitChain();
          _context.SetCurrentConnection(null);
        }

        // testa se o logout retorna true para uma conexão invalidada
        try {
          conn.LoginByPassword(_login, _password, _domain);
          _context.SetCurrentConnection(conn);
          InvalidateLogin(conn);
          Assert.IsTrue(conn.Logout());
          Assert.IsNull(conn.BusId);
          Assert.IsNull(conn.Login);
        }
        finally {
          _context.SetCurrentConnection(null);
        }
      }
    }

    /// <summary>
    /// Testes da auto-propriedade OnInvalidLoginCallback
    ///</summary>
    [TestMethod]
    public void OnInvalidLoginCallbackTest() {
      lock (this) {
        Connection conn = ConnectToBus();
        Assert.IsNull(conn.OnInvalidLogin);
        InvalidLoginCallback callback = InvalidLogin;
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
        Connection conn = ConnectToBus();
        conn.LoginByPassword(_login, _password, _domain);
        Assert.IsNotNull(conn.Login);
        LoginInfo firstLogin = new LoginInfo(conn.Login.Value.id,
                                             conn.Login.Value.entity);
        conn.OnInvalidLogin = InvalidLogin;
        _context.SetDefaultConnection(conn);
        InvalidateLogin(conn);
        // faz uma chamada qualquer para refazer o login
        try {
          _context.OfferRegistry.getAllServices();
          Assert.IsTrue(CallbackCalled);
          Assert.IsFalse(firstLogin.id.Equals(conn.Login.Value.id));
        }
        catch (Exception e) {
          Assert.Fail("O login não foi refeito. Erro: " + e);
        }
        finally {
          CallbackCalled = false;
          _context.SetDefaultConnection(null);
        }
      }
    }

    /// <summary>
    /// Testes de remoção de itens da cache LRU
    ///</summary>
    [TestMethod]
    public void LRUCacheRemoveTest() {
      LRUConcurrentDictionaryCache<Object, string> cache = new LRUConcurrentDictionaryCache<object, string>();
      Object obj1 = new object();
      Object obj12 = new object();
      Object obj13 = new object();
      Object obj2 = new object();
      Object obj22 = new object();
      Object obj3 = new object();
      Object obj4 = new object();
      cache.Set(obj1, "1");
      cache.Set(obj12, "1");
      cache.Set(obj13, "1");
      cache.Set(obj2, "2");
      cache.Set(obj22, "2");
      cache.Set(obj3, "3");
      cache.Set(obj4, "4");
      IEnumerable<Object> keys = cache.RemoveEntriesWithValues(new[] { "1", "2", "5" });
      string temp;
      int size = cache.GetSize();
      Assert.IsTrue((!cache.TryGetValue(obj1, out temp)) &&
                    (!cache.TryGetValue(obj2, out temp)) && (size == 2) &&
                    (keys.Count() == 5));
      cache.RemoveEntriesWithKeys(new[] { obj3, obj4 });
      size = cache.GetSize();
      Assert.IsTrue(size == 0);
    }

    /// <summary>
    /// Cache deve suportar atualizar valor sem remover entradas antigas quando estiver cheia.
    /// </summary>
    [TestMethod]
    public void LRUCacheUpdateValueMustNotRemoveOldest() {
      LRUConcurrentDictionaryCache<string, int> cache = new LRUConcurrentDictionaryCache<string, int>(3);
      cache.Set("a", 1);
      cache.Set("b", 2);
      cache.Set("c", 3);
      cache.Set("c", 4);
      int value;
      Assert.IsTrue(cache.TryGetValue("a", out value));
      Assert.IsTrue(value == 1);
    }

    /// <summary>
    /// Cache deve suportar adicionar ou atualizar valores.
    /// </summary>
    [TestMethod]
    public void LRUCacheAddOrUpdate() {
      LRUConcurrentDictionaryCache<string, int> cache = new LRUConcurrentDictionaryCache<string, int>();
      cache.Set("test", 1);
      cache.Set("test", 2);

      Assert.IsTrue(cache.GetSize() == 1);
      int v;
      Assert.IsTrue(cache.TryGetValue("test", out v));
      Assert.IsTrue(v == 2);
    }

    private void InvalidLogin(Connection conn, LoginInfo login) {
      CallbackCalled = true;
      conn.LoginByPassword(_login, _password, _domain);
    }

    private void InvalidateLogin(Connection conn) {
      IComponent busIC;
      if (_useSSL) {
        busIC =
          (IComponent) OrbServices.CreateProxy(typeof (IComponent), _busIOR);
      }
      else {
        busIC = OrbServices.CreateProxy(
          typeof (IComponent),
          "corbaloc::1.0@" + _hostName + ":" + _hostPort + "/" +
          BusObjectKey.ConstVal)
          as IComponent;
      }
      Assert.IsNotNull(busIC);
      string lrId = Repository.GetRepositoryID(typeof(LoginRegistry));
      LoginRegistry lr = busIC.getFacet(lrId) as LoginRegistry;
      Assert.IsNotNull(lr);
      lr.invalidateLogin(conn.Login.Value.id);
    }

    private static Connection ConnectToBus() {
      return _useSSL
        ? _context.ConnectByReference(_busRef, Props)
        : _context.ConnectByAddress(_hostName, _hostPort, Props);
    }
  }
}