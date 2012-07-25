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
    private static Messenger _messenger;
    private static Broadcaster _broadcaster;
    private static Forwarder _forwarder;

    private static readonly IDictionary<string, ExpectedResult> Expected =
      new Dictionary<string, ExpectedResult>();

    private static readonly IDictionary<string, ExpectedResult> Actual =
      new Dictionary<string, ExpectedResult>();

    private const string William = "willian";
    private const string Bill = "bill";
    private const string Paul = "paul";
    private const string Mary = "mary";
    private const string Steve = "steve";
    private const string TestMessage = "Testing the list!";

    private const string BroadcasterName =
      "interop_delegation_csharp_broadcaster";

    private const string ForwarderName =
      "interop_delegation_csharp_forwarder";

    private static void Main() {
      string hostName = DemoConfig.Default.hostName;
      ushort hostPort = DemoConfig.Default.hostPort;

      IDictionary<string, string> props = new Dictionary<string, string>();
      ConnectionManager manager = ORBInitializer.Manager;
      Connection conn = manager.CreateConnection(hostName, hostPort, props);
      manager.DefaultConnection = conn;

      string userLogin = DemoConfig.Default.userLogin;
      string userPassword = DemoConfig.Default.userPassword;
      ASCIIEncoding encoding = new ASCIIEncoding();
      conn.LoginByPassword(userLogin, encoding.GetBytes(userPassword));

      GetServices(conn);

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
        Actual.Add(name,
                   descs.Length > 0
                     ? new ExpectedResult(name, descs[0].@from, descs[0].message)
                     : null);
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
      foreach (KeyValuePair<string, ExpectedResult> pair in Expected) {
        Assert.IsTrue(Actual.ContainsKey(pair.Key));
        if (pair.Value == null) {
          Assert.IsNull(Actual[pair.Key]);
        }
        else {
          Assert.AreEqual(pair.Value.User, Actual[pair.Key].User);
          Assert.AreEqual(pair.Value.From, Actual[pair.Key].From);
          Assert.AreEqual(pair.Value.Message, Actual[pair.Key].Message);
        }
      }
    }

    private static void FillExpected() {
      Expected.Add(William,
                   new ExpectedResult(William, ForwarderName,
                                      "forwarded message by " + Steve + ":" +
                                      BroadcasterName + ": " + TestMessage));
      Expected.Add(Bill, null);
      Expected.Add(Paul,
                   new ExpectedResult(Paul, Steve + ":" + BroadcasterName,
                                      TestMessage));
      Expected.Add(Mary,
                   new ExpectedResult(Mary, Steve + ":" + BroadcasterName,
                                      TestMessage));
      Expected.Add(Steve,
                   new ExpectedResult(Steve, Steve + ":" + BroadcasterName,
                                      TestMessage));
    }

    private static void ShowPostsOf(string user, PostDesc[] posts) {
      Console.WriteLine(user + " recebeu " + posts.Length + " mensagens:");
      for (int i = 0; i < posts.Length; i++) {
        Console.WriteLine(i + ") " + posts[i].from + ": " + posts[i].message);
      }
      Console.WriteLine();
    }

    private static void GetServices(Connection conn) {
      // propriedade definida pelos servidores
      ServiceProperty prop = new ServiceProperty("offer.domain",
                                                 "Interoperability Tests");

      ServiceProperty[] properties = new[] {prop};
      ServiceOfferDesc[] offers = conn.Offers.findServices(properties);

      if (offers.Length != 3) {
        Console.WriteLine("Há mais serviços do que o esperado no barramento.");
        Console.Read();
        Environment.Exit(1);
      }

      foreach (ServiceOfferDesc serviceOfferDesc in offers) {
        string repId = String.Empty;
        ServiceProperty[] props = serviceOfferDesc.properties;
        foreach (ServiceProperty serviceProperty in props) {
          if (serviceProperty.name.Equals("openbus.component.interface")) {
            repId = serviceProperty.value;
          }
        }
        MarshalByRefObject obj = serviceOfferDesc.service_ref.getFacet(repId);
        if (obj == null) {
          Console.WriteLine("Não foi possível encontrar a faceta do tipo " +
                            repId);
          return;
        }
        Type type = Repository.GetTypeForId(repId);
        if (type == typeof (Messenger)) {
          _messenger = obj as Messenger;
          continue;
        }
        if (type == typeof (Broadcaster)) {
          _broadcaster = obj as Broadcaster;
          continue;
        }
        if (type == typeof (Forwarder)) {
          _forwarder = obj as Forwarder;
        }
      }
    }

    private sealed class ExpectedResult {
      public string User { get; private set; }
      public string From { get; private set; }
      public string Message { get; private set; }

      public ExpectedResult(string user, string from, string message) {
        User = user;
        From = from;
        Message = message;
      }
    }
  }
}