using System.Collections.Concurrent;
using tecgraf.openbus.core.v2_00.credential;
using tecgraf.openbus.core.v2_00.services.access_control;

namespace tecgraf.openbus.sdk {
  internal class CallerChainImpl : CallerChain {
    internal CallerChainImpl(string busId, LoginInfo[] callers,
                             SignedCallChain signed) : this(busId, callers) {
      Signed = signed;
    }

    internal CallerChainImpl(string busId, LoginInfo[] callers) {
      BusId = busId;
      Callers = callers;
      Joined = new ConcurrentDictionary<string, SignedCallChain>();
    }

    internal ConcurrentDictionary<string, SignedCallChain> Joined { get; private set; }

    internal SignedCallChain Signed { get; private set; }

    public string BusId { get; private set; }

    public LoginInfo[] Callers { get; private set; }
  }
}