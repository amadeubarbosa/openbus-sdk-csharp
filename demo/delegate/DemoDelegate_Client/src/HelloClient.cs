using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using DemoDelegate_Client.Properties;
using demoidl.demoDelegate;
using OpenbusAPI;
using OpenbusAPI.Logger;
using OpenbusAPI.Security;
using scs.core;
using tecgraf.openbus.core.v1_05.access_control_service;
using tecgraf.openbus.core.v1_05.registry_service;

namespace DemoDelegate_Client
{
  /// <summary>
  /// Cliente do demo delegate.
  /// </summary>
  class HelloClient
  {
    private static Openbus openbus;

    static void Main(string[] args) {
      AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

      string hostName = DemoConfig.Default.hostName;
      int hostPort = DemoConfig.Default.hostPort;

      Log.setLogsLevel(Level.WARN);

      openbus = Openbus.GetInstance();
      openbus.Init(hostName, hostPort);

      string userLogin = DemoConfig.Default.login;
      string userPassword = DemoConfig.Default.password;
      string privaKeyFile = DemoConfig.Default.xmlPrivateKey;
      string entityName = DemoConfig.Default.entityName;
      string acsCertificateFile = DemoConfig.Default.acsCertificateFileName;

      RSACryptoServiceProvider privateKey = Crypto.ReadPrivateKey(privaKeyFile);
      X509Certificate2 acsCertificate =
        Crypto.ReadCertificate(acsCertificateFile);

      IRegistryService registryService = openbus.Connect(userLogin, userPassword);

      string[] facets = new string[] { "IHello" };
      ServiceOffer[] offers = registryService.find(facets);

      Console.WriteLine("Pressione 'Enter' quando o servidor estiver no ar.");
      Console.ReadLine();

      if (offers.Length < 1) {
        Console.WriteLine("O servi�o Hello n�o se encontra no barramento.");
        Environment.Exit(1);
      }
      if (offers.Length > 1)
        Console.WriteLine("Existe mais de um servi�o Hello no barramento.");

      IComponent component = offers[0].member;
      MarshalByRefObject helloObj = component.getFacetByName("IHello");
      if (helloObj == null) {
        Console.WriteLine("N�o foi poss�vel encontrar uma faceta com esse nome.");
        Environment.Exit(1);
      }

      IHello hello = helloObj as IHello;
      if (hello == null) {
        Console.WriteLine("Faceta encontrada n�o implementa IHello.");
        Environment.Exit(1);
      }

      Thread a = new Thread(DoWork);
      Thread b = new Thread(DoWork);
      a.Start(new DoWorkData("A", hello));
      b.Start(new DoWorkData("B", hello));

      a.Join();
      b.Join();

      Console.WriteLine("Fim");
      Console.ReadLine();
    }

    static void DoWork(Object state) {
      DoWorkData data = (DoWorkData)state;

      String name = data.Name;
      IHello hello = data.IHelloFacet;
      if (String.IsNullOrEmpty(name) || hello == null) {
        Console.WriteLine("Erro! Par�metro state n�o � do tipo DoWorkData");
        Environment.Exit(1);
      }

      Credential credential = openbus.Credential;
      credential._delegate = name;
      openbus.SetThreadCredential(credential);

      for (int i = 0; i < 10; i++) {
        hello.sayHello(name);
        Thread.Sleep(1000);
      }
    }

    /// <summary>
    /// Evento respons�vel por fechar a aplica��o.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    static void CurrentDomain_ProcessExit(object sender, EventArgs e) {
      openbus.Disconnect();
      openbus.Destroy();
    }
  }

  /// <summary>
  /// Estrutura de dados respons�vel por armazenar os par�metros necess�rios
  /// para a Thread DoWork.
  /// </summary>
  struct DoWorkData
  {
    public DoWorkData(String name, IHello helloFacet) {
      _name = name;
      _helloFacet = helloFacet;
    }

    public String Name {
      get { return _name; }
    }
    private String _name;

    public IHello IHelloFacet {
      get { return _helloFacet; }
    }
    private IHello _helloFacet;
  }
}
