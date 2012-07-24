using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace tecgraf.openbus.interop.delegation {
  public class MessengerImpl : MarshalByRefObject, Messenger {
    #region Fields

    private readonly Connection _conn;
    private readonly ConcurrentDictionary<string, List<PostDesc>> _inboxOf;

    #endregion

    #region Constructors

    internal MessengerImpl(Connection conn) {
      _conn = conn;
      _inboxOf = new ConcurrentDictionary<string, List<PostDesc>>();
    }

    #endregion

    public void post(string to, string message) {
      CallerChain chain = _conn.CallerChain;
      string from = chain.Originators[0].entity;
      Console.WriteLine("post to " + to + " by " + from);
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
      string owner = chain.Originators.Length > 0
                       ? chain.Originators[0].entity
                       : chain.Caller.entity;
      Console.WriteLine("download of messsages of " + owner + " by " +
                        ChainToString(chain));
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

    private static string ChainToString(CallerChain chain) {
      string ret = String.Empty;
      for (int i = 0; i < chain.Originators.Length; i++) {
        ret += chain.Originators[i].entity + "->";
      }
      return ret + chain.Caller.entity;
    }
  }
}