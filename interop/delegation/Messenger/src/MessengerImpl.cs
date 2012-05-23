using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using tecgraf.openbus.demo.utils;

namespace tecgraf.openbus.demo.delegation {
  public class MessengerImpl : MarshalByRefObject, Messenger {
    #region Fields

    private readonly Connection _conn;
    private readonly ConcurrentDictionary<string, List<PostDesc>> _inboxOf;

    #endregion

    #region Constructors

    public MessengerImpl(Connection conn) {
      _conn = conn;
      _inboxOf = new ConcurrentDictionary<string, List<PostDesc>>();
    }

    #endregion

    public void post(string to, string message) {
      CallerChain chain = _conn.CallerChain;
      string from = chain.Callers[0].entity;
      Console.WriteLine("post to " + to + " by " + ChainToString.ToString(chain));
      List<PostDesc> inbox;
      if (!_inboxOf.TryGetValue(to, out inbox)) {
        inbox = new List<PostDesc>();
        _inboxOf.TryAdd(to, inbox);
      }
      lock (inbox) {
        inbox.Add(new PostDesc(from, message));
      }
    }

    public PostDesc[] receivePosts() {
      CallerChain chain = _conn.CallerChain;
      string owner = chain.Callers[0].entity;
      Console.WriteLine("download of messsages by " +
                        ChainToString.ToString(chain));
      List<PostDesc> inbox;
      if (_inboxOf.TryRemove(owner, out inbox)) {
        PostDesc[] descs;
        lock (inbox) {
          descs = inbox.ToArray();
        }
        return descs;
      }
      return new PostDesc[0];
    }
  }
}