using Tecgraf.Openbus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Tecgraf.Openbus.sdk;
using tecgraf.openbus.sdk.Exceptions;

namespace Test
{
  /// <summary>
  /// Classe responsável por testar o método de inicializar e finalizar o Openbus.
  /// </summary>
  [TestClass]
  public class OpenbusInitTest
  {
    #region Fields

    private TestContext testContextInstance;
    public TestContext TestContext {
      get {
        return testContextInstance;
      }
      set {
        testContextInstance = value;
      }
    }

    private static String hostName;
    private static int hostPort;

    #endregion

    #region Preparation

    [ClassInitialize]
    public static void BeforeClass(TestContext testContext) {
      hostName = System.Configuration.ConfigurationSettings.AppSettings["hostName"];
      if (String.IsNullOrEmpty(hostName))
        throw new ArgumentNullException("hostName");

      string port = System.Configuration.ConfigurationSettings.AppSettings["hostPort"];
      hostPort = Int32.Parse(port);
    }

    [TestCleanup]
    public void AfterTest() {
      Openbus openbus = Openbus.GetInstance();
      openbus.Destroy();
    }

    #endregion

    #region Tests

    /// <summary>
    /// Testa o método init.
    /// </summary>
    [TestMethod]
    public void Init() {
      Openbus openbus = Openbus.GetInstance();
      openbus.Init(hostName, hostPort);
    }

    /// <summary>
    /// Testa o método init passando o endereço nulo do barramento.
    /// </summary>
    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Init_NullHostName() {
      Openbus openbus = Openbus.GetInstance();
      openbus.Init(null, hostPort);
    }

    /// <summary>
    /// Testa executar o método init duas vezes seguidas.
    /// </summary>
    [TestMethod]
    [ExpectedException(typeof(OpenbusAlreadyInitialized))]
    public void Init_Twice() {
      Openbus openbus = Openbus.GetInstance();
      openbus.Init(hostName, hostPort);

      openbus.Init(hostName, hostPort);
    }

    /// <summary>
    /// Testa o método init passando a porta inválida do barramento.
    /// </summary>
    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Init_InvalidHostPort() {
      Openbus openbus = Openbus.GetInstance();
      openbus.Init(hostName, -1);
    }

    /// <summary>
    /// Testa o método destroy sem inicializar o Openbus
    /// </summary>    
    [TestMethod]
    public void Destroy_WithOutInit() {
      Openbus openbus = Openbus.GetInstance();
      openbus.Destroy();
    }

    #endregion
  }
}
