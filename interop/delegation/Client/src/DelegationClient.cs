using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Ch.Elca.Iiop.Idl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using tecgraf.openbus.core.v2_0.services.offer_registry;
using tecgraf.openbus.interop.delegation.Properties;

namespace tecgraf.openbus.interop.delegation {
  [TestClass]
  internal static class DelegationClient {
    private enum ServerType {
      Unknown,
      Messenger,
      Forwarder,
      Broadcaster
    }

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

    private static string _broadcasterName =
      "interop_delegation_csharp_broadcaster";

    private static string _forwarderName =
      "interop_delegation_csharp_forwarder";

    private static void Main() {
      string hostName = DemoConfig.Default.busHostName;
      ushort hostPort = DemoConfig.Default.busHostPort;

      ConnectionProperties props = new ConnectionPropertiesImpl();
      OpenBusContext context = ORBInitializer.Context;
      Connection conn = context.CreateConnection(hostName, hostPort, props);
      context.SetDefaultConnection(conn);

      const string userLogin = "interop_delegation_csharp_client";
      const string userPassword = userLogin;
      ASCIIEncoding encoding = new ASCIIEncoding();
      conn.LoginByPassword(userLogin, encoding.GetBytes(userPassword));

      GetServices();

      conn.Logout();

      conn.LoginByPassword(Bill, encoding.GetBytes(Bill));
      _forwarder.setForward(William);
      _broadcaster.subscribe();
      conn.Logout();

      conn.LoginByPassword(Paul, encoding.GetBytes(Paul));
      _broadcaster.subscribe();
      conn.Logout();

      conn.LoginByPassword(Mary, encoding.GetBytes(Mary));
      _broadcaster.subscribe();
      conn.Logout();

      conn.LoginByPassword(Steve, encoding.GetBytes(Steve));
      _broadcaster.subscribe();
      _broadcaster.post(TestMessage);
      conn.Logout();

      Console.WriteLine("Esperando as mensagens propagarem.");
      Thread.Sleep(10000);
      Console.WriteLine("Pronto!");

      FillExpected();

      string[] names = new[] {William, Bill, Paul, Mary, Steve};
      foreach (string name in names) {
        conn.LoginByPassword(name, encoding.GetBytes(name));
        PostDesc[] descs = _messenger.receivePosts();
        Actual.Add(name, descs.Length > 0 ? descs : null);
        _broadcaster.unsubscribe();
        conn.Logout();
      }

      conn.LoginByPassword(Bill, encoding.GetBytes(Bill));
      _forwarder.cancelForward(William);
      conn.Logout();
      CheckOutput();
      Console.WriteLine(
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
            Assert.AreEqual(pair.Value[i].from, Actual[pair.Key][i].from);
            Assert.AreEqual(pair.Value[i].message, Actual[pair.Key][i].message);
          }
        }
      }
    }

    private static void FillExpected() {
      PostDesc[] descs = new PostDesc[1];
      descs[0].from = _forwarderName;
      descs[0].message = "forwarded message by " + Steve + "->" +
                         _broadcasterName + ": " + TestMessage;
      Expected.Add(William, descs);
      Expected.Add(Bill, null);
      descs = new PostDesc[1];
      descs[0].from = Steve + "->" + _broadcasterName;
      descs[0].message = TestMessage;
      Expected.Add(Paul, descs);
      Expected.Add(Mary, descs);
      Expected.Add(Steve, descs);
    }

    private static void ShowPostsOf(string user, PostDesc[] posts) {
      Console.WriteLine(user + " recebeu " + posts.Length + " mensagens:");
      for (int i = 0; i < posts.Length; i++) {
        Console.WriteLine(i + ") " + posts[i].from + ": " + posts[i].message);
      }
      Console.WriteLine();
    }

    private static void GetServices() {
      // propriedade definida pelos servidores
      ServiceProperty prop = new ServiceProperty("offer.domain",
                                                 "Interoperability Tests");

      ServiceProperty[] properties = new[] {prop};
      ServiceOfferDesc[] offers =
        ORBInitializer.Context.OfferRegistry.findServices(properties);

      if (offers.Length != 3) {
        Console.WriteLine("Há mais serviços do que o esperado no barramento.");
        Console.Read();
        Environment.Exit(1);
      }

      foreach (ServiceOfferDesc serviceOfferDesc in offers) {
        ServerType serverType = ServerType.Unknown;
        string repId = String.Empty;
        ServiceProperty[] props = serviceOfferDesc.properties;
        foreach (ServiceProperty serviceProperty in props) {
          if (serviceProperty.name.Equals("openbus.component.interface")) {
            repId = serviceProperty.value;
            if (repId.Equals(Repository.GetRepositoryID(typeof (Messenger)))) {
              serverType = ServerType.Messenger;
              break;
            }
            if (repId.Equals(Repository.GetRepositoryID(typeof (Forwarder)))) {
              serverType = ServerType.Forwarder;
              break;
            }
            if (repId.Equals(Repository.GetRepositoryID(typeof (Broadcaster)))) {
              serverType = ServerType.Broadcaster;
              break;
            }
          }
        }

        if (serverType.Equals(ServerType.Unknown)) {
          Console.WriteLine(
            "Uma das ofertas encontradas não é Messenger, Forwarder nem Broadcaster!");
          continue;
        }

        MarshalByRefObject obj = serviceOfferDesc.service_ref.getFacet(repId);
        if (obj == null) {
          Console.WriteLine("Não foi possível encontrar a faceta do tipo " +
                            repId);
          return;
        }

        switch (serverType) {
          case ServerType.Messenger:
            _messenger = obj as Messenger;
            break;
          case ServerType.Broadcaster:
            _broadcaster = obj as Broadcaster;
            foreach (ServiceProperty serviceProperty in props) {
              if (serviceProperty.name.Equals("openbus.offer.entity")) {
                _broadcasterName = serviceProperty.value;
              }
            }
            break;
          case ServerType.Forwarder:
            _forwarder = obj as Forwarder;
            foreach (ServiceProperty serviceProperty in props) {
              if (serviceProperty.name.Equals("openbus.offer.entity")) {
                _forwarderName = serviceProperty.value;
              }
            }
            break;
        }
      }
    }
  }
}
