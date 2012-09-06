using System;
using System.Collections.Generic;
using demo.Properties;
using omg.org.CORBA;
using tecgraf.openbus;
using tecgraf.openbus.core.v2_0.services;
using tecgraf.openbus.core.v2_0.services.offer_registry;

namespace demo {
  /// <summary>
  /// Implementação do servant Callback.
  /// </summary>  
  public class CallbackImpl : MarshalByRefObject, Callback {
    #region Fields

    private readonly string _loginId;
    private readonly Dictionary<string, string> _timerOfferProperties;
    private static volatile int _pending;
    private static readonly object Lock = new object();

    #endregion

    #region Constructors

    public CallbackImpl(string loginId, ServiceOfferDesc timerOffer) {
      _loginId = loginId;
      _timerOfferProperties = new Dictionary<string, string>();
      foreach (ServiceProperty serviceProperty in timerOffer.properties) {
        _timerOfferProperties.Add(serviceProperty.name, serviceProperty.value);
      }
    }

    #endregion

    #region Callback Members

    public void notifyTrigger() {
      OpenBusContext context = ORBInitializer.Context;
      CallerChain chain = context.CallerChain;
      string timerId = _timerOfferProperties["openbus.offer.login"];
      if (timerId.Equals(chain.Caller.id)) {
        Console.WriteLine(Resources.MultiplexingTimerNotificationReceived);
        if (chain.Originators.Length != 1 ||
            !chain.Originators[0].id.Equals(_loginId)) {
          Console.WriteLine(
            Resources.MultiplexingTimerNotificationOutOfOriginalCall);
        }
      }
      else {
        Console.WriteLine(Resources.MultiplexingUnexpectedNotification);
        Console.WriteLine(Resources.MultiplexingUnexpectedNotificationFrom +
                          chain.Caller.id);
        Console.WriteLine(
          Resources.MultiplexingUnexpectedNotificationShouldBeFrom + timerId);
      }
      int temp;
      lock (Lock) {
        --_pending;
        temp = _pending;
      }
      if (temp == 0) {
        try {
          context.GetDefaultConnection().Logout();
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
      }
    }

    #endregion

    internal static void RaisePending() {
      lock (Lock) {
        _pending++;
      }
    }
  }
}