using System;
using omg.org.CORBA;
using tecgraf.openbus.core.v2_00.services.offer_registry;
using tecgraf.openbus.interop.multiplexing.Properties;
using tecgraf.openbus.interop.simple;

namespace tecgraf.openbus.interop.multiplexing {
  public static class Client {
    public static void Main() {
      try {
        string hostName = DemoConfig.Default.hostName;
        short hostPort = DemoConfig.Default.hostPort;
        short hostPort2 = DemoConfig.Default.hostPort2;
        System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();

        ConnectionManager manager = ORBInitializer.Manager;

        Console.WriteLine("Pressione 'Enter' quando o servidor estiver no ar.");
        Console.ReadLine();

        short[] ports = {hostPort, hostPort2};

        foreach (short port in ports) {
          Connection conn = manager.CreateConnection(hostName, port);
          manager.SetDispatcher(conn);
          manager.Requester = conn;
          String login = "interop@" + port;
          conn.LoginByPassword(login, encoding.GetBytes(login));

          ServiceProperty[] serviceProperties = new ServiceProperty[2];
          serviceProperties[0] = new ServiceProperty("openbus.component.facet",
                                                     "Hello");
          serviceProperties[1] = new ServiceProperty("offer.domain",
                                                     "Interoperability Tests");
          ServiceOfferDesc[] services =
            conn.Offers.findServices(serviceProperties);
          foreach (ServiceOfferDesc offer in services) {
            foreach (ServiceProperty prop in offer.properties) {
              if (prop.name.Equals("openbus.offer.entity")) {
                Console.WriteLine("found offer from " + prop.value +
                                  " on bus at port " + port);
              }
            }
            try {
              MarshalByRefObject obj =
                offer.service_ref.getFacet("IDL:tecgraf/openbus/interop/hello/Hello:1.0");
              if (obj == null) {
                Console.WriteLine(
                  "Não foi possível encontrar uma faceta com esse nome.");
                continue;
              }
              Hello hello = obj as Hello;
              if (hello == null) {
                Console.WriteLine("Faceta encontrada não implementa Hello.");
                continue;
              }
              hello.sayHello();
            }
            catch (TRANSIENT) {
              Console.WriteLine(
                "Uma das ofertas obtidas é de um cliente inativo. Tentando a próxima.");
            }
          }
        }
      }
      catch (Exception e) {
        Console.WriteLine(e.StackTrace);
      }
      Console.WriteLine("Pressione qualquer tecla para finalizar.");
      Console.ReadKey();
    }
  }
}