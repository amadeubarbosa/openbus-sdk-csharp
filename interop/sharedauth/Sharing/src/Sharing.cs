using System.IO;
using System.Runtime.Remoting;
using System.Text;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using scs.core;
using tecgraf.openbus.interop.sharedauth.Properties;
using tecgraf.openbus.interop.utils;

namespace tecgraf.openbus.interop.sharedauth {
  /// <summary>
  /// Cliente do teste de interoperabilidade shared auth.
  /// </summary>
  [TestClass]
  internal static class Sharing {
    private static readonly ILog Logger =
      LogManager.GetLogger(typeof(Sharing));

    private static void Main() {
      string hostName = DemoConfig.Default.busHostName;
      ushort hostPort = DemoConfig.Default.busHostPort;
      bool useSSL = DemoConfig.Default.useSSL;
      string clientUser = DemoConfig.Default.clientUser;
      string clientThumbprint = DemoConfig.Default.clientThumbprint;
      string serverUser = DemoConfig.Default.serverUser;
      string serverThumbprint = DemoConfig.Default.serverThumbprint;
      string serverSSLPort = DemoConfig.Default.serverSSLPort;
      string busIORFile = DemoConfig.Default.busIORFile;
      if (useSSL) {
        Utils.InitSSLORB(clientUser, clientThumbprint, serverUser, serverThumbprint, serverSSLPort);
      }
      else {
        ORBInitializer.InitORB();
      }

      ConsoleAppender appender = new ConsoleAppender {
                                                       Threshold = Level.Off,
                                                       Layout =
                                                         new SimpleLayout(),
                                                     };
      BasicConfigurator.Configure(appender);

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

      const string userLogin = "interop_sharedauth_csharp_client";
      byte[] userPassword = new ASCIIEncoding().GetBytes(userLogin);
      string loginFile = DemoConfig.Default.loginFile;

      conn.LoginByPassword(userLogin, userPassword, "testing");
      SharedAuthSecret secret = conn.StartSharedAuth();
      byte[] sharedAuth = context.EncodeSharedAuth(secret);
      File.WriteAllBytes(loginFile, sharedAuth);

      conn.Logout();
      Logger.Info("Fim.");
      Logger.Info(
        "Execute o cliente que fará o login por autenticação compartilhada.");
    }
  }
}