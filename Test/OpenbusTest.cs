using Tecgraf.Openbus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Configuration;
using Tecgraf.Openbus.Lease;
using Tecgraf.Openbus.sdk;
using tecgraf.openbus.core.v1_05.registry_service;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using tecgraf.openbus.core.v1_05.access_control_service;
using tecgraf.openbus.sdk.Exceptions;
using tecgraf.openbus.sdk.Security;

namespace Test
{
  /// <summary>
  /// Classe responsável por testar o método de inicializar e finalizar o Openbus.
  /// </summary>
  [TestClass]
  public class OpenbusTest
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

    private static String userLogin;
    private static String userPassword;
    private static String hostName;
    private static int hostPort;

    private static String testKeyFileName;
    private static String acsCertificateFileName;
    private static String entityName;

    #endregion

    #region Preparation

    [ClassInitialize]
    public static void BeforeClass(TestContext testContext) {
      hostName = System.Configuration.ConfigurationSettings.AppSettings["hostName"];
      if (String.IsNullOrEmpty(hostName))
        throw new ArgumentNullException("hostName");

      string port = System.Configuration.ConfigurationSettings.AppSettings["hostPort"];
      hostPort = Int32.Parse(port);

      userLogin = ConfigurationSettings.AppSettings["userLogin"];
      if (String.IsNullOrEmpty(userLogin))
        throw new ArgumentNullException("userLogin");

      userPassword = ConfigurationSettings.AppSettings["userPassword"];
      if (String.IsNullOrEmpty(userPassword))
        throw new ArgumentNullException("userPassword");

      testKeyFileName = ConfigurationSettings.AppSettings["testKeyFileName"];
      if (String.IsNullOrEmpty(testKeyFileName))
        throw new ArgumentNullException("testKeyFileName");

      acsCertificateFileName = ConfigurationSettings.AppSettings["acsCertificateFileName"];
      if (String.IsNullOrEmpty(acsCertificateFileName))
        throw new ArgumentNullException("acsCertificateFileName");

      entityName = ConfigurationSettings.AppSettings["entityName"];
      if (String.IsNullOrEmpty(entityName))
        throw new ArgumentNullException("entityName");
    }

    /// <summary>
    /// Este método é chamado antes de cada testCase.
    /// </summary>
    [TestInitialize]
    public void BeforeEachTest() {
      Openbus openbus = Openbus.GetInstance();
      openbus.Init(hostName, hostPort);
    }

    /// <summary>
    /// Este método é chamado depois de cada testCase.
    /// </summary>
    [TestCleanup]
    public void AfterEachTest() {
      Openbus openbus = Openbus.GetInstance();
      try { openbus.Disconnect(); }
      catch (Exception) { }
      openbus.Destroy();
    }

    #endregion

    #region Tests

    #region API

    /// <summary>
    /// Testa o connect passando usuário e senha válido.
    /// </summary>
    [TestMethod]
    public void ConnectByPassword() {
      Openbus openbus = Openbus.GetInstance();
      IRegistryService registryService = openbus.Connect(userLogin, userPassword);
      Assert.IsNotNull(registryService);
      Assert.IsTrue(openbus.Disconnect());
    }

    /// <summary>
    /// Testa o connect passando login null.
    /// </summary>
    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void ConnectByPassword_NullLogin() {
      Openbus openbus = Openbus.GetInstance();
      IRegistryService registryService = null;
      registryService = openbus.Connect(null, userPassword);
    }

    /// <summary>
    /// Testa o connect passando password null.
    /// </summary>
    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void ConnectByPassword_NullPassword() {
      Openbus openbus = Openbus.GetInstance();
      IRegistryService registryService = null;
      registryService = openbus.Connect(userLogin, null);
    }

    /// <summary>
    /// Testa o connect passando usuário inválido.
    /// </summary>
    [TestMethod]
    [ExpectedException(typeof(ACSLoginFailureException))]
    public void ConnectByPassword_InvalidLogin() {
      Openbus openbus = Openbus.GetInstance();
      IRegistryService registryService = null;
      registryService = openbus.Connect("null", "null");
    }

    /// <summary>
    /// Testa conectar duas vezes ao barramento utilizando o método connect 
    /// por usuário e senha.
    /// </summary>
    [TestMethod]
    [ExpectedException(typeof(ACSLoginFailureException))]
    public void ConnectByPassword_Twice() {
      Openbus openbus = Openbus.GetInstance();
      IRegistryService registryService = openbus.Connect(userLogin, userPassword);
      Assert.IsNotNull(registryService);
      registryService = null;
      openbus.Connect(userLogin, userPassword);
    }

    /// <summary>
    /// Testa o connect utilizando o certificado.
    /// </summary>
    [TestMethod]
    public void ConnectByCertificate() {
      Openbus openbus = Openbus.GetInstance();
      RSACryptoServiceProvider privateKey = Crypto.ReadPrivateKey(testKeyFileName);
      Assert.IsNotNull(privateKey);

      X509Certificate2 acsCertificate =
        Crypto.ReadCertificate(acsCertificateFileName);
      Assert.IsNotNull(acsCertificate);

      IRegistryService registryService =
        openbus.Connect(entityName, privateKey, acsCertificate);

      Assert.IsNotNull(registryService);
      Assert.IsTrue(openbus.Disconnect());
    }

    /// <summary>
    /// Testa o connect por certificado passando o entity name inválido
    /// </summary>
    [TestMethod]
    [ExpectedException(typeof(ACSLoginFailureException))]
    public void ConnectByCertificate_InvalidEntityName() {
      Openbus openbus = Openbus.GetInstance();
      RSACryptoServiceProvider privateKey = Crypto.ReadPrivateKey(testKeyFileName);
      Assert.IsNotNull(privateKey);

      X509Certificate2 acsCertificate =
        Crypto.ReadCertificate(acsCertificateFileName);
      Assert.IsNotNull(acsCertificate);
      IRegistryService registryService = null;
      registryService = openbus.Connect("null", privateKey, acsCertificate);
    }

    /// <summary>
    /// Testa o connect por certificado passando a cheve privada nula. 
    /// </summary>
    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void ConnectByCertificate_NullKey() {
      Openbus openbus = Openbus.GetInstance();

      X509Certificate2 acsCertificate =
        Crypto.ReadCertificate(acsCertificateFileName);
      Assert.IsNotNull(acsCertificate);
      IRegistryService registryService = null;
      registryService = openbus.Connect(entityName, null, acsCertificate);
    }

    /// <summary>
    /// Testa o connect por certificado passando o certificado digital nulo. 
    /// </summary>
    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void ConnectByCertificate_NullACSCertificate() {
      Openbus openbus = Openbus.GetInstance();
      RSACryptoServiceProvider privateKey = Crypto.ReadPrivateKey(testKeyFileName);
      Assert.IsNotNull(privateKey);

      IRegistryService registryService = null;
      registryService = openbus.Connect(entityName, privateKey, null);
    }

    /// <summary>
    /// Testa conectar duas vezes ao barramento utilizando o método connect 
    /// por certificado
    /// </summary>
    [TestMethod]
    [ExpectedException(typeof(ACSLoginFailureException))]
    public void ConnectByCertificate_Twice() {
      Openbus openbus = Openbus.GetInstance();
      RSACryptoServiceProvider privateKey = Crypto.ReadPrivateKey(testKeyFileName);
      Assert.IsNotNull(privateKey);

      X509Certificate2 acsCertificate =
        Crypto.ReadCertificate(acsCertificateFileName);
      Assert.IsNotNull(acsCertificate);

      IRegistryService registryService =
        openbus.Connect(entityName, privateKey, acsCertificate);
      Assert.IsNotNull(registryService);

      registryService = null;
      registryService = openbus.Connect(entityName, privateKey, acsCertificate);
    }

    /// <summary>
    /// Testa o connect utilizando a credencial.
    /// </summary>
    [TestMethod]
    public void ConnectByCredential() {
      Openbus openbus = Openbus.GetInstance();
      IRegistryService registryService = openbus.Connect(userLogin, userPassword);
      Assert.IsNotNull(registryService);

      Credential myCredential = openbus.Credential;
      registryService = openbus.Connect(myCredential);
      Assert.IsNotNull(registryService);
      Assert.IsTrue(openbus.Disconnect());
    }

    /// <summary>
    /// Testa o connect por credencial passando a credencial nula.
    /// </summary>
    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void ConnectByCredential_NullCredencial() {
      Openbus openbus = Openbus.GetInstance();
      IRegistryService registryService = openbus.Connect(userLogin, userPassword);
      Assert.IsNotNull(registryService);
      registryService = null;

      Credential myCredencial = new Credential();
      registryService = openbus.Connect(myCredencial);
    }

    /// <summary>
    /// Testa o connect por credencial passando a credencial inválida.
    /// </summary>
    [TestMethod]
    [ExpectedException(typeof(InvalidCredentialException))]
    public void ConnectByCredential_InvalidCredencial() {
      Openbus openbus = Openbus.GetInstance();
      IRegistryService registryService = openbus.Connect(userLogin, userPassword);
      Assert.IsNotNull(registryService);
      registryService = null;

      Credential myCredential = new Credential("null", "null", "");
      registryService = openbus.Connect(myCredential);
    }

    /// <summary>
    /// Verifica se uma credencial nula é retornada como credencial interceptada
    /// quando não estamos na thread de uma chamada remota.
    /// </summary>
    [TestMethod]
    public void GetInterceptedCredential() {
      Openbus openbus = Openbus.GetInstance();
      Assert.AreEqual(openbus.GetInterceptedCredential(), new Credential());
    }

    /// <summary>
    /// Testa o IsConnected
    /// </summary>
    [TestMethod]
    public void IsConnected() {
      Openbus openbus = Openbus.GetInstance();
      Assert.IsFalse(openbus.IsConnected());
      Assert.IsNotNull(openbus.Connect(userLogin, userPassword));
      Assert.IsTrue(openbus.IsConnected());
      Assert.IsTrue(openbus.Disconnect());
      Assert.IsFalse(openbus.IsConnected());
    }

    /// <summary>
    /// Testa se disconectar do barramento.
    /// </summary>
    [TestMethod]
    public void Disconnect() {
      Openbus openbus = Openbus.GetInstance();
      Assert.IsFalse(openbus.Disconnect());
      Assert.IsNotNull(openbus.Connect(userLogin, userPassword));
      Assert.IsTrue(openbus.Disconnect());
      Assert.IsFalse(openbus.Disconnect());
    }

    /// <summary>
    /// Testa o GetAccessControlService
    /// </summary>
    [TestMethod]
    public void GetAccessControlService() {
      Openbus openbus = Openbus.GetInstance();
      Assert.IsNull(openbus.GetAccessControlService());
      Assert.IsNotNull(openbus.Connect(userLogin, userPassword));
      Assert.IsNotNull(openbus.GetAccessControlService());
      Assert.IsTrue(openbus.Disconnect());
      Assert.IsNull(openbus.GetAccessControlService());
    }

    /// <summary>
    /// Testa o GetRegistryService
    /// </summary>
    [TestMethod]
    public void GetRegistryService() {
      Openbus openbus = Openbus.GetInstance();
      Assert.IsNull(openbus.GetRegistryService());
      Assert.IsNotNull(openbus.Connect(userLogin, userPassword));
      Assert.IsNotNull(openbus.GetRegistryService());
      Assert.IsTrue(openbus.Disconnect());
      Assert.IsNull(openbus.GetRegistryService());
    }

    #endregion

    #region Private Classes

    /// <summary>
    /// Classe criada para testar o Lease Expired Callback. Utilizada nos
    /// testes unitários <i>addLeaseExpiredCbAfterConnect</i> e 
    /// <i>addLeaseExpiredCbBeforeConnect</i>
    /// </summary>
    private class LeaseExpiredCbReconnect : LeaseExpiredCallback
    {
      private bool reconnected;
      private String userLogin;
      private String userPassword;

      public LeaseExpiredCbReconnect(String userLogin, String userPassword) {
        this.reconnected = false;
        this.userLogin = userLogin;
        this.userPassword = userPassword;
      }

      public bool isReconnected() {
        return this.reconnected;
      }

      void LeaseExpiredCallback.Expired() {
        Openbus openbus = Openbus.GetInstance();
        try {
          openbus.Connect(userLogin, userPassword);
          this.reconnected = true;
        }
        catch (OpenbusException) {
          this.reconnected = false;
        }
      }
    }

    #endregion

    #endregion
  }
}
