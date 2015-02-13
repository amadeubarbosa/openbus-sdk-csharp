﻿using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using tecgraf.openbus.interop.sharedauth.Properties;

namespace tecgraf.openbus.interop.sharedauth {
  /// <summary>
  /// Cliente do teste de interoperabilidade shared auth.
  /// </summary>
  [TestClass]
  internal static class Consuming {
    private static void Main() {
      string hostName = DemoConfig.Default.busHostName;
      ushort hostPort = DemoConfig.Default.busHostPort;
      string loginFile = DemoConfig.Default.loginFile;

      ConsoleAppender appender = new ConsoleAppender {
                                                       Threshold = Level.Info,
                                                       Layout =
                                                         new SimpleLayout(),
                                                     };
      BasicConfigurator.Configure(appender);

      ConnectionProperties props = new ConnectionPropertiesImpl();
      OpenBusContext context = ORBInitializer.Context;
      Connection conn = context.CreateConnection(hostName, hostPort, props);
      context.SetDefaultConnection(conn);

      byte[] encoded = File.ReadAllBytes(loginFile);
      SharedAuthSecret secret = context.DecodeSharedAuth(encoded);
      conn.LoginBySharedAuth(secret);

      Assert.IsNotNull(conn.Login);
      Assert.IsNotNull(conn.Login.Value.id);
      Assert.IsNotNull(conn.Login.Value.entity);

      conn.Logout();
      Console.WriteLine("Fim.");
    }
  }
}