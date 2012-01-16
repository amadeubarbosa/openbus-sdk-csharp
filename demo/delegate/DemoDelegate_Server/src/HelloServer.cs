using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using log4net.Config;
using scs.core;
using Scs.Core;
using Scs.Core.Builder;
using Server.Properties;
using tecgraf.openbus.core.v1_05.registry_service;
using Tecgraf.Openbus;
using Tecgraf.Openbus.Security;

namespace Server
{
  /// <summary>
  /// Servidor do demo hello.
  /// </summary>
  class HelloServer
  {
    static string offerIndentifier;

    static void Main(string[] args) {
      AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);
      string hostName = DemoConfig.Default.hostName;
      int hostPort = DemoConfig.Default.hostPort;

      XmlConfigurator.Configure();

      Openbus openbus = Openbus.GetInstance();
      openbus.Init(hostName, hostPort);

      string entityName = DemoConfig.Default.entityName;
      string privaKeyFile = DemoConfig.Default.xmlPrivateKey;
      string acsCertificateFile = DemoConfig.Default.acsCertificateFileName;

      RSACryptoServiceProvider privateKey = Crypto.ReadPrivateKey(privaKeyFile);
      X509Certificate2 acsCertificate =
        Crypto.ReadCertificate(acsCertificateFile);

      String componentModel = Resources.ComponentModel;
      TextReader file = new StringReader(componentModel);
      XmlTextReader componentInformation = new XmlTextReader(file);
      XmlComponentBuilder builder = new XmlComponentBuilder(componentInformation);
      ComponentContext component = builder.build();

      IRegistryService registryService =
        openbus.Connect(entityName, privateKey, acsCertificate);

      _Property[] properties = new _Property[0];
      IComponent member = component.GetIComponent();
      ServiceOffer serviceOffer = new ServiceOffer(properties, member);
      offerIndentifier = registryService.register(serviceOffer);

      Console.WriteLine("Servidor no ar.");
      openbus.Run();
    }

    static void CurrentDomain_ProcessExit(object sender, EventArgs e) {
      Openbus openbus = Openbus.GetInstance();
      IRegistryService registryService = openbus.GetRegistryService();
      registryService.unregister(offerIndentifier);
      openbus.Disconnect();
      openbus.Destroy();
    }
  }
}
