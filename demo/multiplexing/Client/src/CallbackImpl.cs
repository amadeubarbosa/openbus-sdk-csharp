using System;
using System.Linq;
using System.Threading;
using demo.Properties;
using tecgraf.openbus;
using tecgraf.openbus.core.v2_1.services.offer_registry;

namespace demo {
  /// <summary>
  /// Implementação do servant Callback.
  /// </summary>  
  public class CallbackImpl : MarshalByRefObject, Callback {
    #region Fields

    private readonly string _loginId;
    private readonly string _timerId;
    private readonly Thread _waitingThread;

    #endregion

    #region Constructors

    public CallbackImpl(string loginId, ServiceOfferDesc timerOffer, Thread waitingThread) {
      _loginId = loginId;
      _waitingThread = waitingThread;
      foreach (ServiceProperty serviceProperty in timerOffer.properties.Where(serviceProperty => serviceProperty.name.Equals("openbus.offer.login"))) {
        _timerId = serviceProperty.value;
        break;
      }
      if (_timerId == null) {
        throw new ArgumentException();
      }
    }

    #endregion

    #region Callback Members

    public void notifyTrigger() {
      OpenBusContext context = ORBInitializer.Context;
      CallerChain chain = context.CallerChain;
      if (_timerId.Equals(chain.Caller.id)) {
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
          Resources.MultiplexingUnexpectedNotificationShouldBeFrom + _timerId);
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