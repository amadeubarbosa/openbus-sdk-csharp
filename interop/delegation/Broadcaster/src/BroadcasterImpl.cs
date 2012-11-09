using System;
using System.Collections.Generic;

namespace tecgraf.openbus.interop.delegation {
  public class BroadcasterImpl : MarshalByRefObject, Broadcaster {
    #region Fields

    private readonly Messenger _messenger;
    private readonly List<string> _subscribers;
    private readonly OpenBusContext _context;

    #endregion

    #region Constructors

    internal BroadcasterImpl(Messenger messenger) {
      _messenger = messenger;
      _subscribers = new List<string>();
      _context = ORBInitializer.Context;
    }

    #endregion

    public void post(string message) {
      _context.JoinChain(null);
      lock (_subscribers) {
        foreach (string subscriber in _subscribers) {
          _messenger.post(subscriber, message);
        }
      }
    }

    public void subscribe() {
      CallerChain chain = _context.CallerChain;
      string user = chain.Caller.entity;
      Console.WriteLine("subscription by " + user);
      lock (_subscribers) {
        _subscribers.Add(user);
      }
    }

    public void unsubscribe() {
      CallerChain chain = _context.CallerChain;
      string user = chain.Caller.entity;
      Console.WriteLine("unsubscription by " + user);
      lock (_subscribers) {
        _subscribers.Remove(user);
      }
    }

    public override object InitializeLifetimeService() {
      return null;
    }
  }
}