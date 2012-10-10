using System;
using System.IO;
using System.Text;
using Ch.Elca.Iiop.Idl;
using demo.Properties;
using omg.org.CORBA;
using tecgraf.openbus.assistant;
using tecgraf.openbus.core.v2_0.services.access_control;
using tecgraf.openbus.core.v2_0.services.offer_registry;

namespace demo {
  /// <summary>
  /// Cliente da demo hello.
  /// </summary>
  internal static class HelloClient {
    private static void Main(String[] args) {
      // Obt�m dados atrav�s dos argumentos
      string host = args[0];
      ushort port = Convert.ToUInt16(args[1]);
      string loginFile = args[2];
      string entity = args[3];
      byte[] password = new ASCIIEncoding().GetBytes(args.Length > 4 ? args[4] : entity);

      // Usa o assistente do OpenBus para se conectar ao barramento e realizar a autentica��o.
      Assistant assistant = new AssistantImpl(host, port,
                                              new PasswordProperties(entity,
                                                                     password));

      // inicia o processo de autentica��o compartilhada e serializa os dados
      byte[] secret;
      string loginIOR = assistant.ORB.object_to_string(assistant.StartSharedAuth(out secret, 10));

      // Escreve os dados da autentica��o compartilhada em um arquivo (talvez
      // seja mais interessante para a aplica��o trocar esses dados de outra
      // forma. No m�nimo, essas informa��es deveriam ser encriptadas. Al�m 
      // disso, escreveremos apenas uma vez esses dados, que t�m validade igual
      // ao lease do login atual. Caso o cliente demore a executar, esses dados
      // n�o funcionar�o, portanto uma outra forma mais din�mica seria mais
      // eficaz. No entanto, isso foge ao escopo dessa demo)
      File.WriteAllLines(loginFile, new[] { Convert.ToBase64String(secret), loginIOR });

      // Faz busca utilizando propriedades geradas automaticamente e propriedades definidas pelo servi�o espec�fico
      string helloIDLType = Repository.GetRepositoryID(typeof(Hello));
      // propriedade gerada automaticamente
      ServiceProperty autoProp =
        new ServiceProperty("openbus.component.interface", helloIDLType);
      // propriedade definida pelo servi�o hello
      ServiceProperty prop = new ServiceProperty("offer.domain", "Demo SharedAuth");
      ServiceOfferDesc[] offers =
        Utils.FilterWorkingOffers(assistant.FindServices(
          new[] { prop, autoProp }, 10));

      // utiliza as ofertas encontradas
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
              // utiliza o servi�o
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
  }
}