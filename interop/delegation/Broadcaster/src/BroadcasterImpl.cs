﻿using System;
using System.Collections.Generic;
using tecgraf.openbus.demo.utils;

namespace tecgraf.openbus.interop.delegation {
  public class BroadcasterImpl : MarshalByRefObject, Broadcaster {
    #region Fields

    private readonly Connection _conn;
    private readonly Messenger _messenger;
    private readonly List<string> _subscribers;

    #endregion

    #region Constructors

    public BroadcasterImpl(Connection conn, Messenger messenger) {
      _conn = conn;
      _messenger = messenger;
      _subscribers = new List<string>();
    }

    #endregion

    public void post(string message) {
      _conn.JoinChain(null);
      lock (_subscribers) {
        foreach (string subscriber in _subscribers) {
          _messenger.post(subscriber, message);
        }
      }
    }

    public void subscribe() {
      CallerChain chain = _conn.CallerChain;
      string user = chain.Callers[0].entity;
      Console.WriteLine("subscription by " + ChainToString.ToString(chain));
      lock (_subscribers) {
        _subscribers.Add(user);
      }
    }

    public void unsubscribe() {
      CallerChain chain = _conn.CallerChain;
      string user = chain.Callers[0].entity;
      Console.WriteLine("unsubscription by " + ChainToString.ToString(chain));
      lock (_subscribers) {
        _subscribers.Remove(user);
      }
    }
  }
}