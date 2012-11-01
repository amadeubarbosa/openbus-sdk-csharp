using System;
using System.Text;
using Ch.Elca.Iiop.Idl;
using demo.Properties;
using omg.org.CORBA;
using tecgraf.openbus.assistant;
using tecgraf.openbus.core.v2_0.services.access_control;
using tecgraf.openbus.core.v2_0.services.offer_registry;

namespace demo {
  /// <summary>
  /// Cliente da demo greetings.
  /// </summary>
  internal static class GreetingsClient {
    private static void Main(String[] args) {
      //TODO incluir callbacks de tratamento de erro
      // Obtém dados através dos argumentos
      string host = args[0];
      ushort port = Convert.ToUInt16(args[1]);
      string entity = args[2];
      byte[] password =
        new ASCIIEncoding().GetBytes(args.Length > 3 ? args[3] : entity);

      // Usa o assistente do OpenBus para se conectar ao barramento e realizar a autenticação.
      Assistant assistant = new AssistantImpl(host, port,
                                              new PasswordProperties(entity,
                                                                     password));

      // Pergunta ao usuário qual língua deseja utilizar
      Console.WriteLine(Resources.GreetingsWhichLanguage);
      string language = Console.ReadLine();
      if (language == null) {
        Console.WriteLine(Resources.GreetingsLanguageReadErrorMsg);
        Environment.Exit(1);
      }

      // Faz busca utilizando propriedades geradas automaticamente e propriedades definidas pelo serviço específico
      string greetingsIDLType = Repository.GetRepositoryID(typeof (Greetings));
      // propriedade gerada automaticamente
      ServiceProperty autoProp1 =
        new ServiceProperty("openbus.component.interface", greetingsIDLType);
      // propriedade definida pelo serviço greetings
      ServiceProperty prop = new ServiceProperty("offer.domain",
                                                 "Demo Greetings");
      ServiceProperty[] properties;
      if (!language.Equals("")) {
        ServiceProperty autoProp2 = new ServiceProperty(
          "openbus.component.name", language.ToLower());
        properties = new[] {prop, autoProp1, autoProp2};
      }
      else {
        Console.WriteLine(Resources.GreetingsNoLanguageSpecified);
        properties = new[] {prop, autoProp1};
      }
      ServiceOfferDesc[] offers =
        Utils.FilterWorkingOffers(assistant.FindServices(properties, -1));

      // utiliza as ofertas encontradas
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
              int hours = DateTime.Now.TimeOfDay.Hours;
              MarshalByRefObject greetObj = hours < 12
                                              ? serviceOfferDesc.service_ref.
                                                  getFacetByName("GoodMorning")
                                              : serviceOfferDesc.service_ref.
                                                  getFacetByName(hours >= 18
                                                                   ? "GoodNight"
                                                                   : "GoodAfternoon");
              if (greetObj == null) {
                Console.WriteLine(Resources.FacetNotFoundInOffer);
                continue;
              }
              Greetings greetings = greetObj as Greetings;
              if (greetings == null) {
                Console.WriteLine(Resources.FacetFoundWrongType);
                continue;
              }
              Console.WriteLine(Resources.OfferFound);
              // utiliza o serviço
              Console.WriteLine((string) greetings.sayGreetings());
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
        }
      }

      assistant.Shutdown();
      Console.WriteLine(Resources.ClientOK);
      Console.ReadKey();
    }
  }
}