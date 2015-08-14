using System;
using System.IO;
using System.Threading;
using Ch.Elca.Iiop.Idl;
using log4net;
using omg.org.CORBA;
using Org.BouncyCastle.Crypto;
using Scs.Core;
using scs.core;
using tecgraf.openbus.core.v2_1.services.access_control;
using tecgraf.openbus.core.v2_1.services.offer_registry;
using tecgraf.openbus.exceptions;
using tecgraf.openbus.interop.delegation.Properties;
using tecgraf.openbus.interop.utils;
using tecgraf.openbus.security;

namespace tecgraf.openbus.interop.delegation {
  /// <summary>
  /// Servidor messenger do teste de interoperabilidade delegation.
  /// </summary>
  internal static class MessengerServer {
    private static readonly ILog Logger =
      LogManager.GetLogger(typeof(MessengerServer));

    private const string Entity = "interop_delegation_csharp_messenger";
    private static AsymmetricCipherKeyPair _privateKey;
    private static IComponent _ic;
    private static ServiceProperty[] _properties;
    private static Connection _conn;
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
      ushort serverSSLPort = DemoConfig.Default.serverSSLPort;
      ushort serverOpenPort = DemoConfig.Default.serverOpenPort;
      string busIORFile = DemoConfig.Default.busIORFile;
      if (useSSL) {
        Utils.InitSSLORB(clientUser, clientThumbprint, serverUser, serverThumbprint, serverSSLPort, serverOpenPort, true, true, "required", false, false);
      }
      else {
        ORBInitializer.InitORB();
      }
/*
      ConsoleAppender appender = new ConsoleAppender {
        Threshold = Level.Fatal,
        Layout =
          new SimpleLayout(),
      };
      BasicConfigurator.Configure(appender);
*/
      ConnectionProperties props = new ConnectionPropertiesImpl();
      props.AccessKey = _privateKey;
      OpenBusContext context = ORBInitializer.Context;
      if (useSSL) {
        string ior = File.ReadAllText(busIORFile);
        _conn = context.ConnectByReference((MarshalByRefObject)OrbServices.CreateProxy(typeof(MarshalByRefObject), ior), props);
      }
      else {
        _conn = context.ConnectByAddress(hostName, hostPort, props);
      }
      context.SetDefaultConnection(_conn);

      _conn.LoginByCertificate(Entity, _privateKey);

      ComponentContext component =
        new DefaultComponentContext(new ComponentId("Messenger", 1, 0, 0, ".net"));
      MessengerImpl messenger = new MessengerImpl();
      component.AddFacet("messenger",
                         Repository.GetRepositoryID(typeof (Messenger)),
                         messenger);

      _ic = component.GetIComponent();
      _properties = new[] {
                            new ServiceProperty("offer.domain",
                                                "Interoperability Tests")
                          };
      _offer = context.OfferRegistry.registerService(_ic, _properties);
      _conn.OnInvalidLogin = InvalidLogin;

      Logger.Fatal("Messenger no ar.");

      Thread.Sleep(Timeout.Infinite);
    }

    private static void InvalidLogin(Connection conn, LoginInfo login) {
      try {
        Logger.Fatal(
          "Callback de InvalidLogin foi chamada, tentando logar novamente no barramento.");
        conn.LoginByCertificate(Entity, _privateKey);
        _offer = ORBInitializer.Context.OfferRegistry.registerService(_ic,
                                                                      _properties);
      }
      catch (AlreadyLoggedInException) {
        // outra thread reconectou
      }
      catch (Exception e) {
        Logger.Fatal(e);
      }
    }

    private static void CurrentDomainProcessExit(object sender, EventArgs e) {
      if (_offer != null) {
        try {
          _offer.remove();
        }
        catch (Exception exc) {
          Logger.Fatal(
            "Erro ao remover a oferta antes de finalizar o processo: " + exc);
        }
      }
      _conn.Logout();
    }
  }
}