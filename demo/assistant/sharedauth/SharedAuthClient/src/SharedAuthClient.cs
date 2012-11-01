using System;
using System.IO;
using Ch.Elca.Iiop.Idl;
using demo.Properties;
using omg.org.CORBA;
using tecgraf.openbus;
using tecgraf.openbus.assistant;
using tecgraf.openbus.core.v2_0.services.access_control;
using tecgraf.openbus.core.v2_0.services.offer_registry;

namespace demo {
  internal static class SharedAuthClient {
    private static string _loginFile;

    private static void Main(String[] args) {
      // Obtém dados através dos argumentos
      string host = args[0];
      ushort port = Convert.ToUInt16(args[1]);
      _loginFile = args[2];

      // Usa o assistente do OpenBus para se conectar ao barramento e realizar a autenticação.
      Assistant assistant = new AssistantImpl(host, port,
                                              new SharedAuthProperties(SharedAuthObtainer));

      // Faz busca utilizando propriedades geradas automaticamente e propriedades definidas pelo serviço específico
      string helloIDLType = Repository.GetRepositoryID(typeof(Hello));
      // propriedade gerada automaticamente
      ServiceProperty autoProp =
        new ServiceProperty("openbus.component.interface", helloIDLType);
      // propriedade definida pelo serviço hello
      ServiceProperty prop = new ServiceProperty("offer.domain",
                                                 "Demo SharedAuth");
      ServiceOfferDesc[] offers =
        Utils.FilterWorkingOffers(assistant.FindServices(
          new[] { prop, autoProp }, -1));

      // analiza as ofertas encontradas
      bool failed = true;
      if (offers != null) {
        if (offers.Length < 1) {
          Console.WriteLine(Resources.ServiceNotFound);
        }
        else {
          if (offers.Length > 1) {
            Console.WriteLine(Resources.ServiceFoundMoreThanExpected);
          }
          foreach (ServiceOfferDesc serviceOfferDesc in offers) {
            Console.WriteLine(Resources.ServiceFoundTesting);
            try {
              MarshalByRefObject helloObj =
                serviceOfferDesc.service_ref.getFacet(helloIDLType);
              if (helloObj == null) {
                Console.WriteLine(Resources.FacetNotFoundInOffer);
                continue;
              }
              Hello hello = helloObj as Hello;
              if (hello == null) {
                Console.WriteLine(Resources.FacetFoundWrongType);
                continue;
              }
              Console.WriteLine(Resources.OfferFound);
              // utiliza o serviço
              hello.sayHello();
              failed = false;
              break;
            }
            catch (TRANSIENT) {
              Console.WriteLine(Resources.ServiceTransientErrorMsg);
            }
            catch (COMM_FAILURE) {
              Console.WriteLine(Resources.ServiceCommFailureErrorMsg);
            }
            catch (NO_PERMISSION e) {
              bool found = false;
              string message = String.Empty;
              switch (e.Minor) {
                case NoLoginCode.ConstVal:
                  message = Resources.NoLoginCodeErrorMsg;
                  found = true;
                  break;
                case UnknownBusCode.ConstVal:
                  message = Resources.UnknownBusCodeErrorMsg;
                  found = true;
                  break;
                case UnverifiedLoginCode.ConstVal:
                  message = Resources.UnverifiedLoginCodeErrorMsg;
                  found = true;
                  break;
                case InvalidRemoteCode.ConstVal:
                  message = Resources.InvalidRemoteCodeErrorMsg;
                  found = true;
                  break;
              }
              if (found) {
                Console.WriteLine(message);
              }
              else {
                throw;
              }
            }
          }
          if (failed) {
            Console.WriteLine(Resources.OfferFunctionalNotFound);
          }
        }
      }

      assistant.Shutdown();
      Console.WriteLine(Resources.ClientOK);
      Console.ReadKey();
    }

    private static LoginProcess SharedAuthObtainer(out byte[] secret) {
      // Lê o arquivo com o login process e o segredo (talvez seja mais 
      // interessante para a aplicação trocar esses dados de outra forma.
      // No mínimo, essas informações deveriam estar encriptadas. Além disso, o
      // cliente Hello escreve apenas uma vez esses dados, que têm validade 
      // igual ao lease do login dele, portanto uma outra forma mais dinâmica
      // seria mais eficaz. No entanto, isso foge ao escopo dessa demo)
      OpenBusContext context = ORBInitializer.Context;
      string[] data = File.ReadAllLines(_loginFile);
      secret = Convert.FromBase64String(data[0]);
      return (LoginProcess)context.ORB.string_to_object(data[1]);
    }
  }
}