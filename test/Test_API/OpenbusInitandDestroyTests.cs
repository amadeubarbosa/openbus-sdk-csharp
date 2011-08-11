using System;
using NUnit.Framework;
using Tecgraf.Openbus;
using Tecgraf.Openbus.Logger;
using Tecgraf.Openbus.Exception;


namespace Test_API
{
  /// <summary>
  /// Classe respons�vel por testar o m�todo de inicializar e finalizar o Openbus.
  /// </summary>
  [TestFixture]
  class OpenbusInitandDestroyTests
  {
    #region Fields

    private String hostName;
    private int hostPort;

    #endregion

    #region Constructor

    /// <summary>
    /// Inicializa o teste OpenbusInitTest
    /// </summary>
    public OpenbusInitandDestroyTests() {
      this.hostName = Properties.TestConfig.Default.hostName;
      this.hostPort = Properties.TestConfig.Default.hostPort;
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

    #endregion

    #region Tests

    /// <summary>
    /// Testa o m�todo init.
    /// </summary>
    [Test]
    public void Init() {
      Openbus openbus = Openbus.GetInstance();
      openbus.Init(this.hostName, this.hostPort);
      openbus.Destroy();
    }

    /// <summary>
    /// Testa o m�todo init passando o endere�o nulo do barramento.
    /// </summary>
    [Test]
    [ExpectedException(typeof(ArgumentException))]
    public void Init_NullHostName() {
      Openbus openbus = Openbus.GetInstance();
      try {
        openbus.Init(null, this.hostPort);
      }
      finally {
        openbus.Destroy();
      }
    }

    /// <summary>
    /// Testa executar o m�todo init duas vezes seguidas.
    /// </summary>
    [Test]
    [ExpectedException(typeof(OpenbusAlreadyInitialized))]
    public void Init_Twice() {
      Openbus openbus = Openbus.GetInstance();
      openbus.Init(this.hostName, this.hostPort);
      try {
        openbus.Init(this.hostName, this.hostPort);
      }
      finally {
        openbus.Destroy();
      }
    }

    /// <summary>
    /// Testa o m�todo init passando a porta inv�lida do barramento.
    /// </summary>
    [Test]
    [ExpectedException(typeof(ArgumentException))]
    public void Init_InvalidHostPort() {
      Openbus openbus = Openbus.GetInstance();
      try {
        openbus.Init(this.hostName, -1);
      }
      finally {
        openbus.Destroy();
      }
    }

    /// <summary>
    /// Testa o m�todo destroy sem inicializar o Openbus
    /// </summary>
    [Test]
    public void Destroy_WithOutInit() {
      Openbus openbus = Openbus.GetInstance();
      openbus.Destroy();
    }

    #endregion
  }
}
