using System;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using tecgraf.openbus.interop.sharedauth.Properties;
using tecgraf.openbus.interop.utils;

namespace tecgraf.openbus.interop.sharedauth {
  /// <summary>
  /// Cliente do teste de interoperabilidade shared auth.
  /// </summary>
  [TestClass]
  internal static class Sharing {
    private static void Main() {
      string hostName = DemoConfig.Default.busHostName;
      ushort hostPort = DemoConfig.Default.busHostPort;
      bool useSSL = DemoConfig.Default.useSSL;
      if (useSSL) {
        Utils.InitSSLORB();
      }
      else {
        ORBInitializer.InitORB();
      }

      ConsoleAppender appender = new ConsoleAppender {
                                                       Threshold = Level.Fatal,
                                                       Layout =
                                                         new SimpleLayout(),
                                                     };
      BasicConfigurator.Configure(appender);

      ConnectionProperties props = new ConnectionPropertiesImpl();
      OpenBusContext context = ORBInitializer.Context;
      Connection conn = context.ConnectByAddress(hostName, hostPort, props);
      context.SetDefaultConnection(conn);

      const string userLogin = "interop_sharedauth_csharp_client";
      byte[] userPassword = new ASCIIEncoding().GetBytes(userLogin);
      string loginFile = DemoConfig.Default.loginFile;

      conn.LoginByPassword(userLogin, userPassword, "testing");
      SharedAuthSecret secret = conn.StartSharedAuth();
      byte[] sharedAuth = context.EncodeSharedAuth(secret);
      File.WriteAllBytes(loginFile, sharedAuth);

      conn.Logout();
      Console.WriteLine("Fim.");
      Console.WriteLine(
        "Execute o cliente que fará o login por autenticação compartilhada.");
    }
  }
}