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
  /// Cliente da demo dedicated clock.
  /// </summary>
  internal static class DedicatedClockClient {
    private static void Main(String[] args) {
      //TODO incluir callbacks de tratamento de erro
      // Obtém dados através dos argumentos
      string host = args[0];
      ushort port = Convert.ToUInt16(args[1]);
      string entity = args[2];
      byte[] password =
        new ASCIIEncoding().GetBytes(args.Length > 3 ? args[3] : entity);
      int interval = Convert.ToInt32(args.Length > 4 ? args[4] : "1");
      int retries = Convert.ToInt32(args.Length > 5 ? args[5] : "-1");

      // Usa o assistente do OpenBus para se conectar ao barramento e realizar a autenticação, respeitando um intervalo a cada tentativa.
      AssistantProperties assistantProps = new PasswordProperties(entity,
                                                                  password)
                                           {Interval = interval};
      Assistant assistant = new AssistantImpl(host, port, assistantProps);

      // Faz busca utilizando propriedades geradas automaticamente e propriedades definidas pelo serviço específico.
      string clockIDLType = Repository.GetRepositoryID(typeof (Clock));
      // propriedade gerada automaticamente
      ServiceProperty autoProp =
        new ServiceProperty("openbus.component.interface", clockIDLType);
      // propriedade definida pelo serviço dedicated clock
      ServiceProperty prop = new ServiceProperty("offer.domain",
                                                 "Demo Dedicated Clock");
      ServiceOfferDesc[] offers =
        Utils.FilterWorkingOffers(assistant.FindServices(
          new[] {prop, autoProp}, retries));

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
              MarshalByRefObject clockObj =
                serviceOfferDesc.service_ref.getFacet(clockIDLType);
              if (clockObj == null) {
                Console.WriteLine(Resources.FacetNotFoundInOffer);
                continue;
              }
              Clock clock = clockObj as Clock;
              if (clock == null) {
                Console.WriteLine(Resources.FacetFoundWrongType);
                continue;
              }
              Console.WriteLine(Resources.OfferFound);
              // utiliza o serviço
              long ticks = clock.getTimeInTicks();
              DateTime serverTime = new DateTime(ticks);
              Console.WriteLine(String.Format("Hora do servidor: {0:HH:mm:ss}",
                                              serverTime));
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