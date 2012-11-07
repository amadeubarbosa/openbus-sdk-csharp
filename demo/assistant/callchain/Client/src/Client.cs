﻿using System;
using System.Text;
using Ch.Elca.Iiop.Idl;
using demo.Properties;
using omg.org.CORBA;
using tecgraf.openbus.assistant;
using tecgraf.openbus.core.v2_0.services.access_control;
using tecgraf.openbus.core.v2_0.services.offer_registry;

namespace demo {
  /// <summary>
  /// Cliente da demo CallChain.
  /// </summary>
  internal static class Client {
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

      // Faz busca utilizando propriedades geradas automaticamente e propriedades definidas pelo serviço específico
      string messengerIDLType = Repository.GetRepositoryID(typeof (Messenger));
      // propriedade gerada automaticamente
      ServiceProperty autoProp =
        new ServiceProperty("openbus.component.interface", messengerIDLType);
      // propriedade definida pelo serviço messenger
      ServiceProperty prop = new ServiceProperty("offer.domain",
                                                 "Demo CallChain");
      ServiceOfferDesc[] offers =
        Utils.FilterWorkingOffers(assistant.FindServices(
          new[] {prop, autoProp}, -1));

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
          foreach (ServiceOfferDesc desc in offers) {
            Console.WriteLine(Resources.ServiceFoundTesting);
            try {
              MarshalByRefObject messengerObj =
                desc.service_ref.getFacet(messengerIDLType);
              if (messengerObj == null) {
                Console.WriteLine(Resources.FacetNotFoundInOffer);
                continue;
              }
              Messenger messenger = messengerObj as Messenger;
              if (messenger == null) {
                Console.WriteLine(Resources.FacetFoundWrongType);
                continue;
              }
              Console.WriteLine(Resources.OfferFound);
              // utiliza o serviço
              messenger.showMessage("Hello!");
              failed = false;
              break;
            }
            catch (Unauthorized) {
              Console.WriteLine(
                Resources.CallChainClientServiceRoleErrorMessage +
                Utils.GetProperty(desc.properties, "offer.role") +
                Resources.CallChainClientNotAutorizedMessage);
            }
            catch (Unavailable) {
              Console.WriteLine(
                Resources.CallChainClientServiceRoleErrorMessage +
                Utils.GetProperty(desc.properties, "offer.role") +
                Resources.CallChainClientUnavailableMessage);
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