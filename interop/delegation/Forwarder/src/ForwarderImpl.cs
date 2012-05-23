using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Timers;
using tecgraf.openbus.interop.utils;

namespace tecgraf.openbus.interop.delegation {
  public class ForwarderImpl : MarshalByRefObject, Forwarder {
    #region Fields

    private readonly Connection _conn;
    private readonly ConcurrentDictionary<string, Forward> _forwardsOf;
    private readonly Messenger _messenger;
    public readonly Timer Timer;

    #endregion

    #region Constructors

    public ForwarderImpl(Connection conn, Messenger messenger) {
      _conn = conn;
      _messenger = messenger;
      _forwardsOf = new ConcurrentDictionary<string, Forward>();
      Timer = new Timer(5000);
      Timer.Elapsed += OnTimedEvent;
    }

    #endregion

    #region Forwarder Members

    public void setForward(string to) {
      CallerChain chain = _conn.CallerChain;
      string user = chain.Callers[0].entity;
      Console.WriteLine("setup forward to " + to + " by " + ChainToString.ToString(chain));
      _forwardsOf.TryAdd(user, new Forward(chain, to));
    }

    public void cancelForward(string to) {
      CallerChain chain = _conn.CallerChain;
      string user = chain.Callers[0].entity;
      lock (_forwardsOf) {
        Forward forward;
        if (_forwardsOf.TryGetValue(user, out forward)) {
          Console.WriteLine("cancel forward to " + forward.To + " by " + ChainToString.ToString(chain));
          _forwardsOf.TryRemove(user, out forward);
        }
      }
    }

    public string getForward() {
      CallerChain chain = _conn.CallerChain;
      string user = chain.Callers[0].entity;
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
          string to = keyValuePair.Key;
          Forward f = keyValuePair.Value;
          Console.WriteLine("Checking messages of " + to);
          _conn.JoinChain(f.Chain);
          PostDesc[] posts = _messenger.receivePosts();
          _conn.ExitChain();
          for (int i = 0; i < posts.Length; i++) {
            _messenger.post(to, "forwared from " + posts[i].from + ": " + posts[i].message);
          }
        }
      }
    }

    private class Forward {
      public readonly CallerChain Chain;
      public readonly string To;

      public Forward(CallerChain chain, string to) {
        Chain = chain;
        To = to;
      }
    }
  }
}
