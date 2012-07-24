using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Ch.Elca.Iiop.Idl;
using tecgraf.openbus.core.v2_0.services.offer_registry;
using tecgraf.openbus.interop.delegation.Properties;

namespace tecgraf.openbus.interop.delegation {
  internal static class DelegationClient {
    private static Messenger _messenger;
    private static Broadcaster _broadcaster;
    private static Forwarder _forwarder;

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

      conn.LoginByPassword("bill", encoding.GetBytes("bill"));
      _forwarder.setForward("willian");
      _broadcaster.subscribe();
      conn.Logout();

      conn.LoginByPassword("paul", encoding.GetBytes("paul"));
      _broadcaster.subscribe();
      conn.Logout();

      conn.LoginByPassword("mary", encoding.GetBytes("mary"));
      _broadcaster.subscribe();
      conn.Logout();

      conn.LoginByPassword("steve", encoding.GetBytes("steve"));
      _broadcaster.subscribe();
      _broadcaster.post("Testando a lista!");
      conn.Logout();

      Console.WriteLine("Esperando as mensagens propagarem.");
      Thread.Sleep(10000);
      Console.WriteLine("Pronto!");

      string[] names = new[] {"willian", "bill", "paul", "mary", "steve"};
      foreach (string name in names) {
        conn.LoginByPassword(name, encoding.GetBytes(name));
        ShowPostsOf(name, _messenger.receivePosts());
        _broadcaster.unsubscribe();
        conn.Logout();
      }

      conn.LoginByPassword("bill", encoding.GetBytes("bill"));
      _forwarder.cancelForward("willian");
      conn.Logout();
      Console.WriteLine("Pressione qualquer tecla para terminar.");
      Console.Read();
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
  }
}