using System;
using System.IO;
using System.Threading;
using Ch.Elca.Iiop.Idl;
using omg.org.CORBA;
using Scs.Core;
using scs.core;
using tecgraf.openbus.core.v2_1.services.access_control;
using tecgraf.openbus.core.v2_1.services.offer_registry;
using tecgraf.openbus.exceptions;
using tecgraf.openbus.interop.chaining.Properties;
using tecgraf.openbus.interop.utils;
using tecgraf.openbus.security;

namespace tecgraf.openbus.interop.chaining {
  /// <summary>
  /// Proxy do teste de interoperabilidade chaining.
  /// </summary>
  internal static class ChainingProxy {
    private const string Entity = "interop_chaining_csharp_proxy";
    private static Connection _conn;
    private static PrivateKey _privateKey;
    private static IComponent _ic;
    private static ServiceProperty[] _properties;
    private static ServiceOffer _offer;

    private static void Main() {
      AppDomain.CurrentDomain.ProcessExit += CurrentDomainProcessExit;
      string hostName = DemoConfig.Default.busHostName;
      ushort hostPort = DemoConfig.Default.busHostPort;
      _privateKey = Crypto.ReadKeyFile(DemoConfig.Default.privateKey);
      bool useSSL = DemoConfig.Default.useSSL;
      string clientUser = DemoConfig.Default.clientUser;
      string clientThumbprint = DemoConfig.Default.clientThumbprint;
      string serverUser = DemoConfig.Default.serverUser;
      string serverThumbprint = DemoConfig.Default.serverThumbprint;
      string serverSSLPort = DemoConfig.Default.serverSSLPort;
      string serverOpenPort = DemoConfig.Default.serverOpenPort;
      string busIORFile = DemoConfig.Default.busIORFile;
      if (useSSL) {
        Utils.InitSSLORB(clientUser, clientThumbprint, serverUser, serverThumbprint, serverSSLPort, serverOpenPort);
      }
      else {
        ORBInitializer.InitORB();
      }

      //FileInfo logFileInfo = new FileInfo(DemoConfig.Default.openbusLogFile);
      //XmlConfigurator.ConfigureAndWatch(logFileInfo);

      ConnectionProperties props = new ConnectionPropertiesImpl();
      props.AccessKey = _privateKey;
      OpenBusContext context = ORBInitializer.Context;
      if (useSSL) {
        string ior = File.ReadAllText(busIORFile);
        _conn = context.ConnectByReference((IComponent)OrbServices.CreateProxy(typeof(IComponent), ior), props);
      }
      else {
        _conn = context.ConnectByAddress(hostName, hostPort, props);
      }
      context.SetDefaultConnection(_conn);

      ComponentContext component =
        new DefaultComponentContext(new ComponentId("HelloProxy", 1, 0, 0, ".net"));
      component.AddFacet("HelloProxy", Repository.GetRepositoryID(typeof(HelloProxy)),
                         new ProxyImpl());

      _conn.LoginByCertificate(Entity, _privateKey);

      _ic = component.GetIComponent();
      _properties = new[] {
                            new ServiceProperty("offer.domain",
                                                "Interoperability Tests")
                          };
      _offer = context.OfferRegistry.registerService(_ic, _properties);
      _conn.OnInvalidLogin = InvalidLogin;

      Console.WriteLine("Servidor no ar.");
      Thread.Sleep(Timeout.Infinite);
    }

    private static void CurrentDomainProcessExit(object sender, EventArgs e) {
      if (_offer != null) {
        try {
          _offer.remove();
        }
        catch (Exception exc) {
          Console.WriteLine(
            "Erro ao remover a oferta antes de finalizar o processo: " + exc);
        }
      }
      _conn.Logout();
    }

    private static void InvalidLogin(Connection conn, LoginInfo login) {
      OpenBusContext context = ORBInitializer.Context;
      context.SetCurrentConnection(conn);
      try {
        Console.WriteLine(
          "Callback de InvalidLogin foi chamada, tentando logar novamente no barramento.");
        conn.LoginByCertificate(Entity, _privateKey);
        _offer = context.OfferRegistry.registerService(_ic, _properties);
      }
      catch (AlreadyLoggedInException) {
        // outra thread reconectou
      }
      catch (Exception e) {
        Console.WriteLine(e);
      }
    }
  }
}