﻿using System;
using System.Threading;
using Ch.Elca.Iiop.Idl;
using Messenger.Properties;
using Scs.Core;
using scs.core;
using tecgraf.openbus.core.v2_00.services.offer_registry;
using tecgraf.openbus.sdk;
using tecgraf.openbus.sdk.standard;

namespace Messenger {
  /// <summary>
  /// Servidor do demo hello.
  /// </summary>
  internal static class MessengerServer {
    private static Connection _conn;
    private static ServiceOffer _offer;

    private static void Main(string[] args) {
      AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
      string hostName = DemoConfig.Default.hostName;
      int hostPort = DemoConfig.Default.hostPort;

      OpenBus openbus = StandardOpenBus.Instance;
      _conn = openbus.Connect(hostName, (short)hostPort);

      string userLogin = DemoConfig.Default.userLogin;
      string userPassword = DemoConfig.Default.userPassword;
      System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
      byte[] password = encoding.GetBytes(userPassword);

      _conn.LoginByPassword(userLogin, password);
      _conn.OnInvalidLoginCallback =
        new MessengerInvalidLoginCallback(userLogin, password);

      ComponentContext component =
        new DefaultComponentContext(new ComponentId("Messenger", 1, 0, 0, ".net"));
      //TODO: depois que colocar o getconnection (ou equivalente) no sdk, remover esse parâmetro do construtor
      MessengerImpl messenger = new MessengerImpl(_conn);
      component.AddFacet("messenger",
                         Repository.GetRepositoryID(
                           typeof(tecgraf.openbus.demo.delegation.Messenger)), messenger);

      IComponent member = component.GetIComponent();
      ServiceProperty[] properties = new[] { new ServiceProperty("offer.domain",
                                                                 "OpenBus Demos")
                                           };
      _offer = _conn.OfferRegistry.registerService(member, properties);

      Console.WriteLine("Messenger no ar.");

      Thread.Sleep(Timeout.Infinite);
    }

    private static void CurrentDomain_ProcessExit(object sender, EventArgs e) {
      _offer.remove();
      _conn.Close();
    }
  }
}