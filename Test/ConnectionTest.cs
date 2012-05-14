using tecgraf.openbus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using tecgraf.openbus.core.v2_00.services.access_control;
using tecgraf.openbus.core.v2_00.services.offer_registry;

namespace Test
{
    
    
    /// <summary>
    ///This is a test class for ConnectionTest and is intended
    ///to contain all ConnectionTest Unit Tests
    ///</summary>
  [TestClass()]
  public class ConnectionTest {


    private TestContext testContextInstance;

    /// <summary>
    ///Gets or sets the test context which provides
    ///information about and functionality for the current test run.
    ///</summary>
    public TestContext TestContext {
      get {
        return testContextInstance;
      }
      set {
        testContextInstance = value;
      }
    }

    #region Additional test attributes
    // 
    //You can use the following additional attributes as you write your tests:
    //
    //Use ClassInitialize to run code before running the first test in the class
    //[ClassInitialize()]
    //public static void MyClassInitialize(TestContext testContext)
    //{
    //}
    //
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


    internal virtual Connection CreateConnection() {
      // TODO: Instantiate an appropriate concrete class.
      Connection target = null;
      return target;
    }

    /// <summary>
    ///A test for ExitChain
    ///</summary>
    [TestMethod()]
    public void ExitChainTest() {
      Connection target = CreateConnection(); // TODO: Initialize to an appropriate value
      target.ExitChain();
      Assert.Inconclusive("A method that does not return a value cannot be verified.");
    }

    /// <summary>
    ///A test for JoinChain
    ///</summary>
    [TestMethod()]
    public void JoinChainTest() {
      Connection target = CreateConnection(); // TODO: Initialize to an appropriate value
      CallerChain chain = null; // TODO: Initialize to an appropriate value
      target.JoinChain(chain);
      Assert.Inconclusive("A method that does not return a value cannot be verified.");
    }

    /// <summary>
    ///A test for LoginByCertificate
    ///</summary>
    [TestMethod()]
    public void LoginByCertificateTest() {
      Connection target = CreateConnection(); // TODO: Initialize to an appropriate value
      string entity = string.Empty; // TODO: Initialize to an appropriate value
      byte[] privKey = null; // TODO: Initialize to an appropriate value
      target.LoginByCertificate(entity, privKey);
      Assert.Inconclusive("A method that does not return a value cannot be verified.");
    }

    /// <summary>
    ///A test for LoginByPassword
    ///</summary>
    [TestMethod()]
    public void LoginByPasswordTest() {
      Connection target = CreateConnection(); // TODO: Initialize to an appropriate value
      string entity = string.Empty; // TODO: Initialize to an appropriate value
      byte[] password = null; // TODO: Initialize to an appropriate value
      target.LoginByPassword(entity, password);
      Assert.Inconclusive("A method that does not return a value cannot be verified.");
    }

    /// <summary>
    ///A test for LoginBySingleSignOn
    ///</summary>
    [TestMethod()]
    public void LoginBySingleSignOnTest() {
      Connection target = CreateConnection(); // TODO: Initialize to an appropriate value
      LoginProcess login = null; // TODO: Initialize to an appropriate value
      byte[] secret = null; // TODO: Initialize to an appropriate value
      target.LoginBySingleSignOn(login, secret);
      Assert.Inconclusive("A method that does not return a value cannot be verified.");
    }

    /// <summary>
    ///A test for Logout
    ///</summary>
    [TestMethod()]
    public void LogoutTest() {
      Connection target = CreateConnection(); // TODO: Initialize to an appropriate value
      bool expected = false; // TODO: Initialize to an appropriate value
      bool actual;
      actual = target.Logout();
      Assert.AreEqual(expected, actual);
      Assert.Inconclusive("Verify the correctness of this test method.");
    }

    /// <summary>
    ///A test for StartSingleSignOn
    ///</summary>
    [TestMethod()]
    public void StartSingleSignOnTest() {
      Connection target = CreateConnection(); // TODO: Initialize to an appropriate value
      byte[] secret = null; // TODO: Initialize to an appropriate value
      byte[] secretExpected = null; // TODO: Initialize to an appropriate value
      LoginProcess expected = null; // TODO: Initialize to an appropriate value
      LoginProcess actual;
      actual = target.StartSingleSignOn(out secret);
      Assert.AreEqual(secretExpected, secret);
      Assert.AreEqual(expected, actual);
      Assert.Inconclusive("Verify the correctness of this test method.");
    }

    /// <summary>
    ///A test for BusId
    ///</summary>
    [TestMethod()]
    public void BusIdTest() {
      Connection target = CreateConnection(); // TODO: Initialize to an appropriate value
      string actual;
      actual = target.BusId;
      Assert.Inconclusive("Verify the correctness of this test method.");
    }

    /// <summary>
    ///A test for CallerChain
    ///</summary>
    [TestMethod()]
    public void CallerChainTest() {
      Connection target = CreateConnection(); // TODO: Initialize to an appropriate value
      CallerChain actual;
      actual = target.CallerChain;
      Assert.Inconclusive("Verify the correctness of this test method.");
    }

    /// <summary>
    ///A test for JoinedChain
    ///</summary>
    [TestMethod()]
    public void JoinedChainTest() {
      Connection target = CreateConnection(); // TODO: Initialize to an appropriate value
      CallerChain actual;
      actual = target.JoinedChain;
      Assert.Inconclusive("Verify the correctness of this test method.");
    }

    /// <summary>
    ///A test for Login
    ///</summary>
    [TestMethod()]
    public void LoginTest() {
      Connection target = CreateConnection(); // TODO: Initialize to an appropriate value
      Nullable<LoginInfo> actual;
      actual = target.Login;
      Assert.Inconclusive("Verify the correctness of this test method.");
    }

    /// <summary>
    ///A test for OfferRegistry
    ///</summary>
    [TestMethod()]
    public void OfferRegistryTest() {
      Connection target = CreateConnection(); // TODO: Initialize to an appropriate value
      OfferRegistry actual;
      actual = target.OfferRegistry;
      Assert.Inconclusive("Verify the correctness of this test method.");
    }

    /// <summary>
    ///A test for OnInvalidLoginCallback
    ///</summary>
    [TestMethod()]
    public void OnInvalidLoginCallbackTest() {
      Connection target = CreateConnection(); // TODO: Initialize to an appropriate value
      InvalidLoginCallback expected = null; // TODO: Initialize to an appropriate value
      InvalidLoginCallback actual;
      target.OnInvalidLoginCallback = expected;
      actual = target.OnInvalidLoginCallback;
      Assert.AreEqual(expected, actual);
      Assert.Inconclusive("Verify the correctness of this test method.");
    }
  }
}
