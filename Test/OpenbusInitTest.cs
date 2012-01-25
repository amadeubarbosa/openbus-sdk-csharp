using System;
using System.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using tecgraf.openbus.sdk;
using tecgraf.openbus.sdk.Implementations;

namespace Test {
  /// <summary>
  /// Classe responsável por testar o método de inicializar e finalizar o Openbus.
  /// </summary>
  [TestClass]
  public class OpenbusInitTest {
    #region Fields

    private static String _hostName;
    private static int _hostPort;
    private static String _entity;
    private static byte[] _password;
    public TestContext TestContext { get; set; }

    #endregion

    #region Preparation

    [ClassInitialize]
    public static void BeforeClass(TestContext testContext) {
      _hostName = ConfigurationManager.AppSettings["hostName"];
      if (String.IsNullOrEmpty(_hostName)) {
        throw new ArgumentNullException("hostName");
      }

      string port = ConfigurationManager.AppSettings["hostPort"];
      _hostPort = Int32.Parse(port);

      _entity = ConfigurationManager.AppSettings["entityName"];
      if (String.IsNullOrEmpty(_entity)) {
        throw new ArgumentNullException("entityName");
      }

      string password = ConfigurationManager.AppSettings["userPassword"];
      char[] chars = password.ToCharArray();
      for (int i = 0; i < chars.Length; i++) {
        _password[i] = (byte) chars[i];
      }
    }

    [TestCleanup]
    public void AfterTest() {
    }

    #endregion

    #region Tests

    /// <summary>
    /// Testa a inicialização da classe.
    /// </summary>
    [TestMethod]
    public void Instantiate() {
      Openbus bus = new StandardOpenbus(_hostName, _hostPort, false);
      Connection conn = bus.Connect();
      conn.LoginByPassword(_entity, _password);
      conn.Logout();
    }

    /// <summary>
    /// Testa a inicialização passando o endereço do barramento nulo.
    /// </summary>
    [TestMethod]
    [ExpectedException(typeof (ArgumentException))]
    public void InstantiateNullHostName() {
      new StandardOpenbus(null, _hostPort, false);
    }

    /// <summary>
    /// Testa a inicialização de dois barramentos.
    /// </summary>
    [TestMethod]
    public void InstantiateTwice() {
      new StandardOpenbus(_hostName, _hostPort, false);
      new StandardOpenbus(_hostName, _hostPort, false);
    }

    /// <summary>
    /// Testa a inicialização passando uma porta inválida.
    /// </summary>
    [TestMethod]
    [ExpectedException(typeof (ArgumentException))]
    public void InstantiateInvalidHostPort() {
      new StandardOpenbus(_hostName, -1, false);
    }

    #endregion
  }
}