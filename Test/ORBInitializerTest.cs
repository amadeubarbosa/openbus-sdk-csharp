using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace tecgraf.openbus.Test {
  /// <summary>
  ///This is a test class for ORBInitializerTest and is intended
  ///to contain all ORBInitializerTest Unit Tests
  ///</summary>
  [TestClass]
  public class ORBInitializerTest {
    /// <summary>
    ///Gets or sets the test context which provides
    ///information about and functionality for the current test run.
    ///</summary>
    public TestContext TestContext { get; set; }

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

    /// <summary>
    /// Teste da auto-propriedade Manager
    ///</summary>
    [TestMethod]
    [MethodImpl(MethodImplOptions.Synchronized)]
    [DeploymentItem("Openbus.dll")]
    public void ManagerTest() {
      Assert.IsNotNull(ORBInitializer.Manager);
    }
  }
}