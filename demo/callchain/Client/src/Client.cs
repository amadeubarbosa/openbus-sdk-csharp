using System;
using System.Reflection;
using System.Text;
using Ch.Elca.Iiop.Idl;
using demo.Properties;
using omg.org.CORBA;
using tecgraf.openbus;
using tecgraf.openbus.core.v2_0.services;
using tecgraf.openbus.core.v2_0.services.access_control;
using tecgraf.openbus.core.v2_0.services.offer_registry;

namespace demo {
  /// <summary>
  /// Cliente da demo CallChain.
  /// </summary>
  internal static class Client {
    private static void Main(String[] args) {
      // Obtém dados através dos argumentos
      string host = args[0];
      ushort port = Convert.ToUInt16(args[1]);
      string entity = args[2];
      byte[] password = new ASCIIEncoding().GetBytes(args.Length > 3 ? args[3] : entity);

      // Cria conexão e a define como conexão padrão tanto para entrada como saída.
      // O uso exclusivo da conexão padrão (sem uso de current e callback de despacho) só é recomendado para aplicações que criem apenas uma conexão e desejem utilizá-la em todos os casos. Para situações diferentes, consulte o manual do SDK OpenBus e/ou outras demos.
      OpenBusContext context = ORBInitializer.Context;
      Connection conn = context.CreateConnection(host, port);
      context.SetDefaultConnection(conn);

      string messengerIDLType = Repository.GetRepositoryID(typeof (Messenger));
      ServiceOfferDesc[] offers = null;
      try {
        // Faz o login
        conn.LoginByPassword(entity, password);
        // Faz busca utilizando propriedades geradas automaticamente e propriedades definidas pelo serviço específico
        // propriedade gerada automaticamente
        ServiceProperty autoProp =
          new ServiceProperty("openbus.component.interface", messengerIDLType);
        // propriedade definida pelo serviço messenger
        ServiceProperty prop = new ServiceProperty("offer.domain",
                                                   "Demo CallChain");
        ServiceProperty[] properties = {prop, autoProp};
        offers = context.OfferRegistry.findServices(properties);
      }
      catch (AccessDenied) {
        Console.WriteLine(Resources.ClientAccessDenied + entity + ".");
      }
      catch (ServiceFailure e) {
        Console.WriteLine(Resources.BusServiceFailureErrorMsg);
        Console.WriteLine(e);
      }
      catch (TRANSIENT) {
        Console.WriteLine(Resources.BusTransientErrorMsg);
      }
      catch (COMM_FAILURE) {
        Console.WriteLine(Resources.BusCommFailureErrorMsg);
      }
      catch (Exception e) {
        NO_PERMISSION npe = null;
        if (e is TargetInvocationException) {
          // caso seja uma exceção lançada pelo SDK, será uma NO_PERMISSION
          npe = e.InnerException as NO_PERMISSION;
        }
        if ((npe == null) && (!(e is NO_PERMISSION))) {
          // caso não seja uma NO_PERMISSION não é uma exceção esperada então deixamos passar.
          throw;
        }
        npe = npe ?? e as NO_PERMISSION;
        if (npe.Minor == NoLoginCode.ConstVal) {
          Console.WriteLine(Resources.NoLoginCodeErrorMsg);
        }
        else {
          throw;
        }
      }

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
                GetProperty(desc, "offer.role") +
                Resources.CallChainClientNotAutorizedMessage);
            }
            catch (Unavailable) {
              Console.WriteLine(
                Resources.CallChainClientServiceRoleErrorMessage +
                GetProperty(desc, "offer.role") +
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

      try {
        conn.Logout();
      }
      catch (ServiceFailure e) {
        Console.WriteLine(Resources.BusServiceFailureErrorMsg);
        Console.WriteLine(e);
      }
      catch (TRANSIENT) {
        Console.WriteLine(Resources.BusTransientErrorMsg);
      }
      catch (COMM_FAILURE) {
        Console.WriteLine(Resources.BusCommFailureErrorMsg);
      }
      if (!failed) {
        Console.WriteLine(Resources.ClientOK);
      }
      Console.ReadKey();
    }

    private static string GetProperty(ServiceOfferDesc offer, string name) {
      foreach (ServiceProperty property in offer.properties) {
        if (property.name.Equals(name)) {
          return property.value;
        }
      }
      return null;
    }
  }
}