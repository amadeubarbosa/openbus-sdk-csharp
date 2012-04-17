using System;
using MultiplexingClient.Properties;
using demoidl.hello;
using tecgraf.openbus.core.v2_00.services.offer_registry;
using tecgraf.openbus.sdk;

namespace MultiplexingClient
{
  public static class Client
  {
    public static void Main(string[] args)
    {
      try
      {
        string hostName = DemoConfig.Default.hostName;
        int hostPort = DemoConfig.Default.hostPort;
        int hostPort2 = DemoConfig.Default.hostPort2;
        System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();

        ConnectionManager manager = ORBInitializer.Manager;

        Console.WriteLine("Pressione 'Enter' quando o servidor estiver no ar.");
        Console.ReadLine();

        short[] ports = {(short) hostPort, (short) hostPort2};

        foreach (short port in ports)
        {
          Connection conn = manager.CreateConnection(hostName, port);
          manager.SetupBusDispatcher(conn);
          manager.ThreadRequester = conn;
          String login = "demo@" + port;
          conn.LoginByPassword(login, encoding.GetBytes(login));

          ServiceProperty[] serviceProperties = new ServiceProperty[2];
          serviceProperties[0] = new ServiceProperty("openbus.component.facet", "Hello");
          serviceProperties[1] = new ServiceProperty("offer.domain", "OpenBus Demos");
          ServiceOfferDesc[] services = conn.OfferRegistry.findServices(serviceProperties);
          foreach (ServiceOfferDesc offer in services)
          {
            foreach (ServiceProperty prop in offer.properties)
            {
              if (prop.name.Equals("openbus.offer.entity"))
              {
                Console.WriteLine("found offer from " + prop.value + " on bus at port " + port);
              }
            }
            MarshalByRefObject obj = offer.service_ref.getFacetByName("Hello");
            IHello hello = obj as IHello;
            hello.sayHello();
          }
        }
      }
      catch (Exception e)
      {
        Console.WriteLine(e.StackTrace);
      }
    }
  }
}
