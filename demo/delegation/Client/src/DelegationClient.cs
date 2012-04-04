using System;
using System.Threading;
using Ch.Elca.Iiop.Idl;
using Client.Properties;
using tecgraf.openbus.core.v2_00.services.offer_registry;
using tecgraf.openbus.demo.delegation;
using tecgraf.openbus.sdk;
using tecgraf.openbus.sdk.standard;

namespace Client {
  internal static class DelegationClient {
    private static Connection _conn;
    private static Messenger _messenger;
    private static Broadcaster _broadcaster;
    private static Forwarder _forwarder;

    private static void Main(string[] args) {
      AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
      string hostName = DemoConfig.Default.hostName;
      int hostPort = DemoConfig.Default.hostPort;

      OpenBus openbus = StandardOpenBus.Instance;
      _conn = openbus.Connect(hostName, (short) hostPort);

      string userLogin = DemoConfig.Default.userLogin;
      string userPassword = DemoConfig.Default.userPassword;
      System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
      _conn.LoginByPassword(userLogin, encoding.GetBytes(userPassword));

      GetServices();

      _conn.Logout();

      _conn.LoginByPassword("willian", encoding.GetBytes("willian"));
      _forwarder.setForward("bill");
      _broadcaster.subscribe();
      _conn.Logout();

      _conn.LoginByPassword("paul", encoding.GetBytes("paul"));
      _broadcaster.subscribe();
      _conn.Logout();

      _conn.LoginByPassword("mary", encoding.GetBytes("mary"));
      _broadcaster.subscribe();
      _conn.Logout();

      _conn.LoginByPassword("steve", encoding.GetBytes("steve"));
      _broadcaster.subscribe();
      _broadcaster.post("Testando a lista!");
      _conn.Logout();

      Console.WriteLine("Esperando as mensagens propagarem.");
      Thread.Sleep(10000);
      Console.WriteLine("Pronto!");

      string[] names = new[]{"willian", "bill", "paul", "mary", "steve"};
      foreach (string name in names) {
        _conn.LoginByPassword(name, encoding.GetBytes(name));
        ShowPostsOf(name, _messenger.receivePosts());
        _broadcaster.unsubscribe();
        _conn.Logout();
      }
      
      _conn.LoginByPassword("willian", encoding.GetBytes("willian"));
      _forwarder.cancelForward("bill");
      _conn.Logout();
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

    private static void GetServices() {
      // propriedade definida pelos servidores
      ServiceProperty prop = new ServiceProperty("offer.domain", "OpenBus Demos");

      ServiceProperty[] properties = new[] {prop};
      ServiceOfferDesc[] offers = _conn.OfferRegistry.findServices(properties);

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

    private static void CurrentDomain_ProcessExit(object sender, EventArgs e) {
      _conn.Close();
    }
  }
}