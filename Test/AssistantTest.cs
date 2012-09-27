using System;
using System.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using tecgraf.openbus.assistant;
using tecgraf.openbus.assistant.properties;
using tecgraf.openbus.core.v2_0.services.access_control;
using tecgraf.openbus.exceptions;
using tecgraf.openbus.security;

namespace tecgraf.openbus.test {
  /// <summary>
  ///This is a test class for ConnectionTest and is intended
  ///to contain all Assistant Unit Tests
  /// </summary>
  [TestClass]
  public class AssistantTest {
    #region Fields

    private static String _hostName;
    private static ushort _hostPort;
    private static String _entity;
    private static String _entityNoCert;
    private static string _login;
    private static byte[] _password;
    private static PrivateKey _privKey;
    private static PrivateKey _wrongKey;
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

    public AssistantTest() {
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
      _privKey = Crypto.ReadKeyFile(privateKey);
      Props.AccessKey = _privKey;

      string wrongKey = ConfigurationManager.AppSettings["wrongKeyFileName"];
      if (String.IsNullOrEmpty(password)) {
        throw new ArgumentNullException("wrongKeyFileName");
      }
      _wrongKey = Crypto.ReadKeyFile(wrongKey);

      _context = ORBInitializer.Context;
    }

    #region Additional test attributes
    //
    // You can use the following additional attributes as you write your tests:
    //
    // Use ClassInitialize to run code before running the first test in the class
    // [ClassInitialize()]
    // public static void MyClassInitialize(TestContext testContext) { }
    //
    // Use ClassCleanup to run code after all tests in a class have run
    // [ClassCleanup()]
    // public static void MyClassCleanup() { }
    //
    // Use TestInitialize to run code before running each test 
    // [TestInitialize()]
    // public void MyTestInitialize() { }
    //
    // Use TestCleanup to run code after each test has run
    // [TestCleanup()]
    // public void MyTestCleanup() { }
    //
    #endregion

    private static LoginProcess StartSharedAuth(out byte[] secret) {
      return ORBInitializer.Context.GetCurrentConnection().StartSharedAuth(out secret);
    }

    private class AssistantPropertiesMock : AssistantPropertiesImpl {
    }

    [TestMethod]
    public void CreateAssistantPropertiesTest() {
      bool failed = false;
      PasswordProperties passProps = new PasswordProperties(_entity, _password);
      try {
        passProps.Interval = -2;
      }
      catch (InvalidPropertyValueException e) {
        if (e.Property.Equals("interval")) {
          failed = true;
        }
      }
      if (!failed) {
        Assert.Fail(
          "A instanciação do assistente funcionou com um tipo de realização de login inválido.");
      }
      //TODO valores válidos nas propriedades opcionais
    }

    [TestMethod]
    public void CreateAssistantTest() {
      lock (_context) {
        bool failed = false;
        // cria com senha
        new AssistantImpl(_hostName, _hostPort, new PasswordProperties(_entity, _password));
        // cria com chave privada
        new AssistantImpl(_hostName, _hostPort, new PrivateKeyProperties(_entity, _privKey));
        // cria com autenticação compartilhada
        new AssistantImpl(_hostName, _hostPort, new SharedAuthProperties(StartSharedAuth));
        // cria com uma implementação diferente
        try {
          new AssistantImpl(_hostName, _hostPort, new AssistantPropertiesMock());
        }
        catch(ArgumentException) {
          failed = true;
        }
        if (!failed) {
          Assert.Fail("A instanciação do assistente funcionou com um tipo de realização de login inválido.");
        }
      }
    }
  }
}
