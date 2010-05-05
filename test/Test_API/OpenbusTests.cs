using System;
using NUnit.Framework;
using OpenbusAPI.Logger;
using OpenbusAPI;
using OpenbusAPI.Exception;
using OpenbusAPI.Security;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using OpenbusAPI.Lease;
using tecgraf.openbus.core.v1_05.registry_service;
using tecgraf.openbus.core.v1_05.access_control_service;

namespace Test_API
{
  /// <summary>
  /// Classe respons�vel por testar a API Openbus.
  /// </summary>
  [TestFixture]
  class OpenbusTests
  {
    #region Fields

    private String userLogin;
    private String userPassword;
    private String hostName;
    private int hostPort;

    private String testKeyFileName;
    private String acsCertificateFileName;
    private String entityName;

    #endregion

    #region Consts

    /// <summary> O tempo em segundos do lease do Barramento. </summary>
    private const int LEASE_TIME = 60;

    #endregion

    #region Constructor

    /// <summary>
    /// Inicializa o teste OpenbusTest
    /// </summary>
    public OpenbusTests() {

      this.userLogin = Properties.TestConfig.Default.userLogin;
      this.userPassword = Properties.TestConfig.Default.userPassword;
      this.hostName = Properties.TestConfig.Default.hostName;
      this.hostPort = Properties.TestConfig.Default.hostPort;
      this.testKeyFileName = Properties.TestConfig.Default.testKeyFileName;
      this.acsCertificateFileName = Properties.TestConfig.Default.acsCertificateFileName;
      this.entityName = Properties.TestConfig.Default.entityName;
    }

    #endregion

    #region Preparation

    /// <summary>
    /// Este m�todo � chamado antes de todos os testCases.
    /// </summary>
    [TestFixtureSetUp]
    public void BeforeTests() {
      Log.setLogsLevel(Level.WARN);
    }

    /// <summary>
    /// Este m�todo � chamado antes de cada testCase.
    /// </summary>
    [SetUp]
    public void BeforeEachTest() {
      Openbus openbus = Openbus.GetInstance();
      openbus.Init(hostName, hostPort);
    }

    /// <summary>
    /// Este m�todo � chamado depois de cada testCase.
    /// </summary>
    [TearDown]
    public void AfterEachTest() {
      Openbus openbus = Openbus.GetInstance();
      openbus.Destroy();
    }

    #endregion

    #region Tests

    #region API

    /// <summary>
    /// Testa o connect passando usu�rio e senha v�lido.
    /// </summary>
    [Test]
    public void ConnectByPassword() {
      Openbus openbus = Openbus.GetInstance();
      IRegistryService registryService = openbus.Connect(userLogin, userPassword);
      Assert.NotNull(registryService);
      Assert.True(openbus.Disconnect());
    }

    /// <summary>
    /// Testa o connect passando login null.
    /// </summary>
    [Test]
    [ExpectedException(typeof(ArgumentException))]
    public void ConnectByPassword_NullLogin() {
      Openbus openbus = Openbus.GetInstance();
      IRegistryService registryService = null;
      try {
        registryService = openbus.Connect(null, userPassword);
      }
      finally {
        Assert.Null(registryService);
        Assert.False(openbus.Disconnect());
      }
    }

    /// <summary>
    /// Testa o connect passando password null.
    /// </summary>
    [Test]
    [ExpectedException(typeof(ArgumentException))]
    public void ConnectByPassword_NullPassword() {
      Openbus openbus = Openbus.GetInstance();
      IRegistryService registryService = null;
      try {
        registryService = openbus.Connect(userLogin, null);
      }
      finally {
        Assert.Null(registryService);
        Assert.False(openbus.Disconnect());
      }
    }

    /// <summary>
    /// Testa o connect passando usu�rio inv�lido.
    /// </summary>
    [Test]
    [ExpectedException(typeof(ACSLoginFailureException))]
    public void ConnectByPassword_InvalidLogin() {
      Openbus openbus = Openbus.GetInstance();
      IRegistryService registryService = null;
      try {
        registryService = openbus.Connect("null", "null");
      }
      finally {
        Assert.Null(registryService);
        Assert.False(openbus.Disconnect());
      }
    }

    /// <summary>
    /// Testa conectar duas vezes ao barramento utilizando o m�todo connect 
    /// por usu�rio e senha.
    /// </summary>
    [Test]
    [ExpectedException(typeof(ACSLoginFailureException))]
    public void ConnectByPassword_Twice() {
      Openbus openbus = Openbus.GetInstance();
      IRegistryService registryService = openbus.Connect(userLogin, userPassword);
      Assert.NotNull(registryService);
      registryService = null;
      try {
        openbus.Connect(userLogin, userPassword);
      }
      finally {
        Assert.Null(registryService);
        Assert.True(openbus.Disconnect());
      }
    }

    /// <summary>
    /// Testa o connect utilizando o certificado.
    /// </summary>
    [Test]
    public void ConnectByCertificate() {
      Openbus openbus = Openbus.GetInstance();
      RSACryptoServiceProvider privateKey = Crypto.ReadPrivateKey(testKeyFileName);
      Assert.IsNotNull(privateKey);

      X509Certificate2 acsCertificate =
        Crypto.ReadCertificate(acsCertificateFileName);
      Assert.IsNotNull(acsCertificate);

      IRegistryService registryService =
        openbus.Connect(entityName, privateKey, acsCertificate);
      Assert.NotNull(registryService);
      Assert.True(openbus.Disconnect());
    }

    /// <summary>
    /// Testa o connect por certificado passando o entity name inv�lido
    /// </summary>
    [Test]
    [ExpectedException(typeof(ACSLoginFailureException))]
    public void ConnectByCertificate_InvalidEntityName() {
      Openbus openbus = Openbus.GetInstance();
      RSACryptoServiceProvider privateKey = Crypto.ReadPrivateKey(testKeyFileName);
      Assert.IsNotNull(privateKey);

      X509Certificate2 acsCertificate =
        Crypto.ReadCertificate(acsCertificateFileName);
      Assert.IsNotNull(acsCertificate);
      IRegistryService registryService = null;
      try {
        registryService = openbus.Connect("null", privateKey, acsCertificate);
      }
      finally {
        Assert.Null(registryService);
        Assert.False(openbus.Disconnect());
      }
    }

    /// <summary>
    /// Testa o connect por certificado passando a cheve privada nula. 
    /// </summary>
    [Test]
    [ExpectedException(typeof(ArgumentException))]
    public void ConnectByCertificate_NullKey() {
      Openbus openbus = Openbus.GetInstance();

      X509Certificate2 acsCertificate =
        Crypto.ReadCertificate(acsCertificateFileName);
      Assert.IsNotNull(acsCertificate);

      IRegistryService registryService = null;
      try {
        registryService = openbus.Connect(entityName, null, acsCertificate);
      }
      finally {
        Assert.Null(registryService);
        Assert.False(openbus.Disconnect());
      }
    }

    /// <summary>
    /// Testa o connect por certificado passando o certificado digital nulo. 
    /// </summary>
    [Test]
    [ExpectedException(typeof(ArgumentException))]
    public void ConnectByCertificate_NullACSCertificate() {
      Openbus openbus = Openbus.GetInstance();
      RSACryptoServiceProvider privateKey = Crypto.ReadPrivateKey(testKeyFileName);
      Assert.IsNotNull(privateKey);

      IRegistryService registryService = null;
      try {
        registryService = openbus.Connect(entityName, privateKey, null);
      }
      finally {
        Assert.Null(registryService);
        Assert.False(openbus.Disconnect());
      }
    }

    /// <summary>
    /// Testa conectar duas vezes ao barramento utilizando o m�todo connect 
    /// por certificado
    /// </summary>
    [Test]
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
      Assert.NotNull(registryService);

      registryService = null;
      try {
        registryService = openbus.Connect(entityName, privateKey, acsCertificate);
      }
      finally {
        Assert.Null(registryService);
        Assert.True(openbus.Disconnect());
      }
    }

    /// <summary>
    /// Testa o connect utilizando a credencial.
    /// </summary>
    [Test]
    public void ConnectByCredential() {
      Openbus openbus = Openbus.GetInstance();
      IRegistryService registryService = openbus.Connect(userLogin, userPassword);
      Assert.NotNull(registryService);

      Credential myCredential = openbus.Credential;
      registryService = openbus.Connect(myCredential);
      Assert.NotNull(registryService);
      Assert.True(openbus.Disconnect());
    }

    /// <summary>
    /// Testa o connect por credencial passando a credencial nula.
    /// </summary>
    [Test]
    [ExpectedException(typeof(ArgumentException))]
    public void ConnectByCredential_NullCredencial() {
      Openbus openbus = Openbus.GetInstance();
      IRegistryService registryService = openbus.Connect(userLogin, userPassword);
      Assert.NotNull(registryService);
      registryService = null;

      Credential myCredencial = new Credential();
      try {
        registryService = openbus.Connect(myCredencial);
      }
      finally {
        Assert.Null(registryService);
        Assert.True(openbus.Disconnect());
      }
    }

    /// <summary>
    /// Testa o connect por credencial passando a credencial inv�lida.
    /// </summary>
    [Test]
    [ExpectedException(typeof(InvalidCredentialException))]
    public void ConnectByCredential_InvalidCredencial() {
      Openbus openbus = Openbus.GetInstance();
      IRegistryService registryService = openbus.Connect(userLogin, userPassword);
      Assert.NotNull(registryService);
      registryService = null;

      Credential myCredential = new Credential("null", "null", "");
      try {
        registryService = openbus.Connect(myCredential);
      }
      finally {
        Assert.Null(registryService);
        Assert.True(openbus.Disconnect());
      }
    }

    /// <summary>
    /// Testa o IsConnected
    /// </summary>
    [Test]
    public void IsConnected() {
      Openbus openbus = Openbus.GetInstance();
      Assert.False(openbus.IsConnected());
      Assert.NotNull(openbus.Connect(userLogin, userPassword));
      Assert.True(openbus.IsConnected());
      Assert.True(openbus.Disconnect());
      Assert.False(openbus.IsConnected());
    }

    /// <summary>
    /// Testa se disconectar do barramento.
    /// </summary>
    [Test]
    public void Disconnect() {
      Openbus openbus = Openbus.GetInstance();
      Assert.False(openbus.Disconnect());
      Assert.NotNull(openbus.Connect(userLogin, userPassword));
      Assert.True(openbus.Disconnect());
      Assert.False(openbus.Disconnect());
    }

    /// <summary>
    /// Testa o GetAccessControlService
    /// </summary>
    [Test]
    public void GetAccessControlService() {
      Openbus openbus = Openbus.GetInstance();
      Assert.Null(openbus.GetAccessControlService());
      Assert.NotNull(openbus.Connect(userLogin, userPassword));
      Assert.NotNull(openbus.GetAccessControlService());
      Assert.True(openbus.Disconnect());
      Assert.Null(openbus.GetAccessControlService());
    }

    /// <summary>
    /// Testa o GetRegistryService
    /// </summary>
    [Test]
    public void GetRegistryService() {
      Openbus openbus = Openbus.GetInstance();
      Assert.Null(openbus.GetRegistryService());
      Assert.NotNull(openbus.Connect(userLogin, userPassword));
      Assert.NotNull(openbus.GetRegistryService());
      Assert.True(openbus.Disconnect());
      Assert.Null(openbus.GetRegistryService());
    }

    /// <summary>
    /// Testa o GetSessionService
    /// </summary>
    [Test]
    public void GetSessionService() {
      Openbus openbus = Openbus.GetInstance();
      Assert.Null(openbus.GetSessionService());
      Assert.NotNull(openbus.Connect(userLogin, userPassword));
      Assert.NotNull(openbus.GetSessionService());
      Assert.True(openbus.Disconnect());
      Assert.Null(openbus.GetSessionService());
    }

    /// <summary>
    /// Teste o expiredCallback 
    /// </summary>
    [Test]
    [Timeout(2 * LEASE_TIME * 1000)]
    public void LeaseExpireCredencial() {
      Openbus openbus = Openbus.GetInstance();
      IRegistryService registryService = openbus.Connect(userLogin, userPassword);
      Assert.NotNull(registryService);
      LeaseExpiredCallbackImpl callback = new LeaseExpiredCallbackImpl();
      openbus.SetLeaseExpiredCallback(callback);
      IAccessControlService acs = openbus.GetAccessControlService();
      Assert.True(acs.logout(openbus.Credential));
      while (!callback.isExpired()) {
        ;
      }
    }

    /// <summary>
    /// Testa a utiliza��o de uma callback respons�vel por reconectar ap�s a
    /// expira��o da credencial. O cadastro dessa lease acontece antes do
    /// m�todo connect.
    /// </summary>
    [Test]
    [Timeout(2 * LEASE_TIME * 1000)]
    public void AddLeaseExpiredCbBeforeConnect() {
      Openbus openbus = Openbus.GetInstance();
      LeaseExpiredCbReconnect callback = new LeaseExpiredCbReconnect(userLogin, userPassword);
      openbus.SetLeaseExpiredCallback(callback);
      IRegistryService registryService = openbus.Connect(userLogin, userPassword);
      Credential credential = openbus.Credential;
      Assert.IsNotNullOrEmpty(credential.identifier);
      Assert.NotNull(registryService);
      IAccessControlService acs = openbus.GetAccessControlService();
      Assert.True(acs.logout(openbus.Credential));
      while (!callback.isReconnected()) {
        ;
      }
      Credential newCredential = openbus.Credential;
      Assert.False(credential.identifier.Equals(newCredential.identifier));
    }

    /// <summary>
    /// Testa a utiliza��o de uma callback respons�vel por reconectar ap�s a
    /// expira��o da credencial. O cadastro dessa lease acontece depois do
    /// m�todo connect.
    /// </summary>
    [Test]
    [Timeout(10 * LEASE_TIME * 1000)]
    public void addLeaseExpiredCbAfterConnect() {
      Openbus openbus = Openbus.GetInstance();
      IRegistryService registryService = openbus.Connect(userLogin, userPassword);
      Credential credential = openbus.Credential;
      Assert.IsNotNullOrEmpty(credential.identifier);
      LeaseExpiredCbReconnect callback = new LeaseExpiredCbReconnect(userLogin, userPassword);
      openbus.SetLeaseExpiredCallback(callback);
      Assert.NotNull(registryService);
      IAccessControlService acs = openbus.GetAccessControlService();
      Assert.True(acs.logout(openbus.Credential));
      while (!callback.isReconnected()) {
        ;
      }
      Credential newCredential = openbus.Credential;
      Assert.False(credential.identifier.Equals(newCredential.identifier));
    }

    #endregion

    #region Internal Members

    /// <summary>
    /// Classe criada para testar o Lease Expired Callback. Utilizada no teste 
    /// unit�rio <i>LeaseExpireCredencial</i>.
    /// </summary>
    private class LeaseExpiredCallbackImpl : LeaseExpiredCallback
    {
      private volatile bool expired;

      public LeaseExpiredCallbackImpl() {
        this.expired = false;
      }

      public void Expired() {
        this.expired = true;
      }

      public bool isExpired() {
        return this.expired;
      }
    }

    /// <summary>
    /// Classe criada para testar o Lease Expired Callback. Utilizada nos
    /// testes unit�rios <i>addLeaseExpiredCbAfterConnect</i> e 
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
