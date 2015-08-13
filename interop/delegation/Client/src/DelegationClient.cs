using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Ch.Elca.Iiop.Idl;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using omg.org.CORBA;
using scs.core;
using tecgraf.openbus.core.v2_1.services.offer_registry;
using tecgraf.openbus.interop.delegation.Properties;
using tecgraf.openbus.interop.utils;

namespace tecgraf.openbus.interop.delegation {
  [TestClass]
  internal static class DelegationClient {
    private static readonly ILog Logger =
      LogManager.GetLogger(typeof(DelegationClient));

    private static Messenger _messenger;
    private static Broadcaster _broadcaster;
    private static Forwarder _forwarder;

    private static readonly IDictionary<string, PostDesc[]> Expected =
      new Dictionary<string, PostDesc[]>();

    private static readonly IDictionary<string, PostDesc[]> Actual =
      new Dictionary<string, PostDesc[]>();

    private const string William = "willian";
    private const string Bill = "bill";
    private const string Paul = "paul";
    private const string Mary = "mary";
    private const string Steve = "steve";
    private const string TestMessage = "Testing the list!";

    private const string BroadcasterName = "interop_delegation_(cpp|java|lua|csharp)_broadcaster";

    private const string ForwarderName = "interop_delegation_(cpp|java|lua|csharp)_forwarder";

    private static void Main() {
      string hostName = DemoConfig.Default.busHostName;
      ushort hostPort = DemoConfig.Default.busHostPort;
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
      OpenBusContext context = ORBInitializer.Context;
      Connection conn;
      if (useSSL) {
        string ior = File.ReadAllText(busIORFile);
        conn = context.ConnectByReference((IComponent)OrbServices.CreateProxy(typeof(IComponent), ior), props);
      }
      else {
        conn = context.ConnectByAddress(hostName, hostPort, props);
      }
      context.SetDefaultConnection(conn);

      const string userLogin = "interop_delegation_csharp_client";
      const string userPassword = userLogin;
      ASCIIEncoding encoding = new ASCIIEncoding();
      conn.LoginByPassword(userLogin, encoding.GetBytes(userPassword), "testing");

      GetService(typeof(Messenger));
      GetService(typeof(Forwarder));
      GetService(typeof(Broadcaster));

      conn.Logout();

      conn.LoginByPassword(Bill, encoding.GetBytes(Bill), "testing");
      _forwarder.setForward(William);
      _broadcaster.subscribe();
      conn.Logout();

      conn.LoginByPassword(Paul, encoding.GetBytes(Paul), "testing");
      _broadcaster.subscribe();
      conn.Logout();

      conn.LoginByPassword(Mary, encoding.GetBytes(Mary), "testing");
      _broadcaster.subscribe();
      conn.Logout();

      conn.LoginByPassword(Steve, encoding.GetBytes(Steve), "testing");
      _broadcaster.subscribe();
      _broadcaster.post(TestMessage);
      conn.Logout();

      Logger.Fatal("Esperando as mensagens propagarem.");
      Thread.Sleep(10000);
      Logger.Fatal("Pronto!");

      FillExpected();

      string[] names = {William, Bill, Paul, Mary, Steve};
      foreach (string name in names) {
        conn.LoginByPassword(name, encoding.GetBytes(name), "testing");
        PostDesc[] descs = _messenger.receivePosts();
        Actual.Add(name, descs.Length > 0 ? descs : null);
        _broadcaster.unsubscribe();
        conn.Logout();
      }

      conn.LoginByPassword(Bill, encoding.GetBytes(Bill), "testing");
      _forwarder.cancelForward(William);
      conn.Logout();
      CheckOutput();
      Logger.Fatal(
        "Teste de interoperabilidade Delegation executado com êxito.");
    }

    private static void CheckOutput() {
      Assert.AreEqual(Expected.Count, Actual.Count);
      foreach (KeyValuePair<string, PostDesc[]> pair in Expected) {
        Assert.IsTrue(Actual.ContainsKey(pair.Key));
        if (pair.Value == null) {
          Assert.IsNull(Actual[pair.Key]);
        }
        else {
          Assert.AreEqual(pair.Value.Length, Actual[pair.Key].Length);
          // for abaixo depende de ordem estar correta, mas para o exemplo atual funciona.
          for (int i = 0; i < pair.Value.Length; i++) {
            Assert.IsTrue(Regex.IsMatch(Actual[pair.Key][i].from, pair.Value[i].from));
            Assert.IsTrue(Regex.IsMatch(Actual[pair.Key][i].message, pair.Value[i].message));
          }
        }
      }
    }

    private static void FillExpected() {
      PostDesc[] descs = new PostDesc[1];
      descs[0].from = ForwarderName;
      descs[0].message = "forwarded message by " + Steve + "->" +
                         BroadcasterName + ": " + TestMessage;
      Expected.Add(William, descs);
      Expected.Add(Bill, null);
      descs = new PostDesc[1];
      descs[0].from = Steve + "->" + BroadcasterName;
      descs[0].message = TestMessage;
      Expected.Add(Paul, descs);
      Expected.Add(Mary, descs);
      Expected.Add(Steve, descs);
    }

/*
    private static void ShowPostsOf(string user, PostDesc[] posts) {
      Logger.Fatal(user + " recebeu " + posts.Length + " mensagens:");
      for (int i = 0; i < posts.Length; i++) {
        Logger.Fatal(i + ") " + posts[i].from + ": " + posts[i].message);
      }
      Logger.Fatal("\n");
    }
*/
    private static void GetService(Type type) {
      // propriedades geradas automaticamente
      ServiceProperty autoProp =
        new ServiceProperty("openbus.component.interface",
                            Repository.GetRepositoryID(type));
      // propriedade definida pelo servidor hello
      ServiceProperty prop = new ServiceProperty("offer.domain",
                                                 "Interoperability Tests");

      ServiceProperty[] properties = { autoProp, prop };
      List<ServiceOfferDesc> offers =
        Utils.FindOffer(ORBInitializer.Context.OfferRegistry, properties, 1, 10, 1);

      foreach (ServiceOfferDesc serviceOfferDesc in offers) {
        try {
          MarshalByRefObject obj =
            serviceOfferDesc.service_ref.getFacet(
              Repository.GetRepositoryID(type));
          if (obj == null) {
            Logger.Fatal(
              "Não foi possível encontrar uma faceta com esse nome.");
            continue;
          }

          if (type == typeof (Messenger)) {
            Messenger facet = obj as Messenger;
            if (facet != null) {
              _messenger = facet;
              return;
            }
          }
          if (type == typeof(Forwarder)) {
            Forwarder facet = obj as Forwarder;
            if (facet != null) {
              _forwarder = facet;
              return;
            }
          }
          if (type == typeof(Broadcaster)) {
            Broadcaster facet = obj as Broadcaster;
            if (facet != null) {
              _broadcaster = facet;
              return;
            }
          }
        }
        catch (TRANSIENT) {
          Logger.Fatal(
            "Uma das ofertas obtidas é de um cliente inativo. Tentando a próxima.");
        }
      }
      Assert.Fail("Um servidor do tipo " + type + " não foi encontrado.");
    }
  }
}
