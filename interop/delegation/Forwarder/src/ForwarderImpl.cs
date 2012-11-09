﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Timers;

namespace tecgraf.openbus.interop.delegation {
  public class ForwarderImpl : MarshalByRefObject, Forwarder {
    #region Fields

    private readonly ConcurrentDictionary<string, Forward> _forwardsOf;
    private readonly Messenger _messenger;
    internal readonly Timer Timer;

    #endregion

    #region Constructors

    internal ForwarderImpl(Messenger messenger) {
      _messenger = messenger;
      _forwardsOf = new ConcurrentDictionary<string, Forward>();
      Timer = new Timer(5000);
      Timer.Elapsed += OnTimedEvent;
      Timer.Enabled = true;
    }

    #endregion

    #region Forwarder Members

    public void setForward(string to) {
      CallerChain chain = ORBInitializer.Context.CallerChain;
      string user = chain.Caller.entity;
      Console.WriteLine("setup forward to " + to + " by " + user);
      _forwardsOf.TryAdd(user, new Forward(chain, to));
    }

    public void cancelForward(string to) {
      CallerChain chain = ORBInitializer.Context.CallerChain;
      string user = chain.Caller.entity;
      lock (_forwardsOf) {
        Forward forward;
        if (_forwardsOf.TryGetValue(user, out forward)) {
          Console.WriteLine("cancel forward to " + forward.To + " by " + user);
          _forwardsOf.TryRemove(user, out forward);
        }
      }
    }

    public string getForward() {
      CallerChain chain = ORBInitializer.Context.CallerChain;
      string user = chain.Caller.entity;
      Forward forward;
      if (_forwardsOf.TryGetValue(user, out forward)) {
        return forward.To;
      }
      throw new NoForward();
    }

    #endregion

    private void OnTimedEvent(object source, ElapsedEventArgs e) {
      lock (_forwardsOf) {
        foreach (KeyValuePair<string, Forward> keyValuePair in _forwardsOf) {
          string user = keyValuePair.Key;
          Forward f = keyValuePair.Value;
          Console.WriteLine("Checking messages of " + user);
          OpenBusContext context = ORBInitializer.Context;
          context.JoinChain(f.Chain);
          PostDesc[] posts = _messenger.receivePosts();
          context.ExitChain();
          for (int i = 0; i < posts.Length; i++) {
            _messenger.post(f.To,
                            "forwarded message by " + posts[i].from + ": " +
                            posts[i].message);
          }
        }
      }
    }

    public override object InitializeLifetimeService() {
      return null;
    }

    private class Forward {
      internal readonly CallerChain Chain;
      internal readonly string To;

      internal Forward(CallerChain chain, string to) {
        Chain = chain;
        To = to;
      }
    }
  }
}