using System.IO;
using System.Reflection;
using System.Runtime.Remoting;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using scs.core;
using tecgraf.openbus.interop.sharedauth.Properties;
using tecgraf.openbus.interop.utils;

namespace tecgraf.openbus.interop.sharedauth {
  /// <summary>
  /// Cliente do teste de interoperabilidade shared auth.
  /// </summary>
  [TestClass]
  internal static class Consuming {
    private static readonly ILog Logger =
      LogManager.GetLogger(typeof(Consuming));

    private static void Main() {
      Assembly.Load("OpenBus.Legacy.Idl");
      string hostName = DemoConfig.Default.busHostName;
      ushort hostPort = DemoConfig.Default.busHostPort;
      string loginFile = DemoConfig.Default.loginFile;
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
                                                       Threshold = Level.Off,
                                                       Layout =
                                                         new SimpleLayout(),
                                                     };
      BasicConfigurator.Configure(appender);
*/
      ConnectionProperties props = new ConnectionPropertiesImpl();
      OpenBusContext context = ORBInitializer.Context;
      Connection conn;
      if (useSSL) {
        string ior = File.ReadAllText(busIORFile);
        conn = context.ConnectByReference((IComponent)RemotingServices.Connect(typeof(IComponent), ior), props);
      }
      else {
        conn = context.ConnectByAddress(hostName, hostPort, props);
      }
      context.SetDefaultConnection(conn);

      byte[] encoded = File.ReadAllBytes(loginFile);
      SharedAuthSecret secret = context.DecodeSharedAuth(encoded);
      conn.LoginBySharedAuth(secret);

      Assert.IsNotNull(conn.Login);
      Assert.IsNotNull(conn.Login.Value.id);
      Assert.IsNotNull(conn.Login.Value.entity);

      conn.Logout();
      Logger.Info("Fim.");
    }
  }
}