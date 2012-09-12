using System;
using Ch.Elca.Iiop.Idl;
using audit;
using demo.Properties;
using omg.org.CORBA;
using tecgraf.openbus;
using tecgraf.openbus.core.v2_0.services.access_control;
using tecgraf.openbus.core.v2_0.services.offer_registry;

namespace demo {
  /// <summary>
  /// Implementação do servant Messenger do proxy.
  /// </summary>  
  public class ProxyMessengerImpl : MarshalByRefObject, Messenger {
    private readonly string _messengerType =
      Repository.GetRepositoryID(typeof (Messenger));

    #region Messenger Members

    public void showMessage(string message) {
      CallerChain chain = ORBInitializer.Context.CallerChain;
      Console.WriteLine(Resources.CallChainProxyForwardingMessage +
                        ChainToString.ToString(chain));
      OpenBusContext context = ORBInitializer.Context;
      context.JoinChain(chain);
      foreach (ServiceOfferDesc offer in Proxy.Offers) {
        try {
          MarshalByRefObject messengerObj =
            offer.service_ref.getFacet(_messengerType);
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
          messenger.showMessage(message);
          return;
        }
        catch (Unauthorized) {
        }
        catch (Unavailable) {
        }
        catch (TRANSIENT) {
          Console.WriteLine(Resources.ServiceTransientErrorMsg);
        }
        catch (COMM_FAILURE) {
          Console.WriteLine(Resources.ServiceCommFailureErrorMsg);
        }
        catch (NO_PERMISSION e) {
          bool found = false;
          string msg = String.Empty;
          switch (e.Minor) {
            case NoLoginCode.ConstVal:
              msg = Resources.NoLoginCodeErrorMsg;
              found = true;
              break;
            case UnknownBusCode.ConstVal:
              msg = Resources.UnknownBusCodeErrorMsg;
              found = true;
              break;
            case UnverifiedLoginCode.ConstVal:
              msg = Resources.UnverifiedLoginCodeErrorMsg;
              found = true;
              break;
            case InvalidRemoteCode.ConstVal:
              msg = Resources.InvalidRemoteCodeErrorMsg;
              found = true;
              break;
          }
          if (found) {
            Console.WriteLine(msg);
          }
          else {
            throw;
          }
        }
      }
      Console.WriteLine(Resources.CallChainProxyUnavailableMessage);
      throw new Unavailable();
    }

    #endregion
  }
}