using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tecgraf.Openbus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Configuration;
using Tecgraf.Openbus.Lease;
using Tecgraf.Openbus.sdk;
using tecgraf.openbus.core.v1_05.registry_service;
using tecgraf.openbus.core.v1_05.access_control_service;
using tecgraf.openbus.sdk.Exceptions;

namespace Test
{

  [TestClass]
  public class OpenbusLeaseTest
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

    #endregion

    #region Consts

    /// <summary> O tempo em segundos do lease do Barramento. </summary>
    private const int LEASE_TIME = 2;

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
      openbus.Destroy();
    }

    #endregion

    #region Test

    /// <summary>
    /// Teste o expiredCallback
    /// </summary>
    [TestMethod]
    [Timeout(120000)]
    public void LeaseExpireCredencial() {
      Openbus openbus = Openbus.GetInstance();
      IRegistryService registryService = openbus.Connect(userLogin, userPassword);
      Assert.IsNotNull(registryService);
      LeaseExpiredCallbackImpl callback = new LeaseExpiredCallbackImpl();
      openbus.SetLeaseExpiredCallback(callback);
      IAccessControlService acs = openbus.GetAccessControlService();
      Assert.IsTrue(acs.logout(openbus.Credential));
      while (!callback.isExpired()) {
        ;
      }
    }

    /// <summary>
    /// Testa a utilização de uma callback responsável por reconectar após a
    /// expiração da credencial. O cadastro dessa lease acontece antes do
    /// método connect.
    /// </summary>
    [TestMethod]
    [Timeout(120000)]
    public void AddLeaseExpiredCbBeforeConnect() {
      Openbus openbus = Openbus.GetInstance();
      LeaseExpiredCbReconnect callback = new LeaseExpiredCbReconnect(userLogin, userPassword);
      openbus.SetLeaseExpiredCallback(callback);
      IRegistryService registryService = openbus.Connect(userLogin, userPassword);
      Credential credential = openbus.Credential;
      Assert.IsFalse(credential.identifier == String.Empty);
      Assert.IsNotNull(registryService);
      IAccessControlService acs = openbus.GetAccessControlService();
      Assert.IsTrue(acs.logout(openbus.Credential));
      while (!callback.isReconnected()) {
        ;
      }
      Credential newCredential = openbus.Credential;
      Assert.IsFalse(credential.identifier.Equals(newCredential.identifier));
    }

    /// <summary>
    /// Testa a utilização de uma callback responsável por reconectar após a
    /// expiração da credencial. O cadastro dessa lease acontece depois do
    /// método connect.
    /// </summary>
    [TestMethod]
    [Timeout(120000)]
    public void AddLeaseExpiredCbAfterConnect() {
      Openbus openbus = Openbus.GetInstance();
      IRegistryService registryService = openbus.Connect(userLogin, userPassword);
      Credential credential = openbus.Credential;
      Assert.IsFalse(credential.identifier == String.Empty);
      LeaseExpiredCbReconnect callback = new LeaseExpiredCbReconnect(userLogin, userPassword);
      openbus.SetLeaseExpiredCallback(callback);
      Assert.IsNotNull(registryService);
      IAccessControlService acs = openbus.GetAccessControlService();
      Assert.IsTrue(acs.logout(openbus.Credential));
      while (!callback.isReconnected()) {
        ;
      }
      Credential newCredential = openbus.Credential;
      Assert.IsFalse(credential.identifier.Equals(newCredential.identifier));
    }

    #endregion

    #region Internal Members

    /// <summary>
    /// Classe criada para testar o Lease Expired Callback. Utilizada no teste 
    /// unitário <i>LeaseExpireCredencial</i>.
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
    /// testes unitários <i>addLeaseExpiredCbAfterConnect</i> e 
    /// <i>addLeaseExpiredCbBeforeConnect</i>
    /// </summary>
    private class LeaseExpiredCbReconnect : LeaseExpiredCallback
    {
      private volatile bool reconnected;
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
  }
}
