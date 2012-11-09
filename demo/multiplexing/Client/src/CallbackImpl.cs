using System;
using System.Collections.Generic;
using System.Threading;
using demo.Properties;
using tecgraf.openbus;
using tecgraf.openbus.core.v2_0.services.offer_registry;

namespace demo {
  /// <summary>
  /// Implementação do servant Callback.
  /// </summary>  
  public class CallbackImpl : MarshalByRefObject, Callback {
    #region Fields

    private readonly string _loginId;
    private readonly Dictionary<string, string> _timerOfferProperties;
    private readonly Thread _waitingThread;

    #endregion

    #region Constructors

    public CallbackImpl(string loginId, ServiceOfferDesc timerOffer, Thread waitingThread) {
      _loginId = loginId;
      _waitingThread = waitingThread;
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
      --MultiplexingClient.Pending;
      if (MultiplexingClient.Pending == 0) {
        _waitingThread.Interrupt();
      }
    }

    #endregion

    public override object InitializeLifetimeService() {
      return null;
    }
  }
}