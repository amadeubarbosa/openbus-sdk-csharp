using System.Collections.Concurrent;
using tecgraf.openbus.core.v2_00.services.access_control;

namespace tecgraf.openbus.sdk
{
  internal class CallerChainImpl : CallerChain {
    internal CallerChainImpl(string busId, LoginInfo[] callers, SignedCallChain signed) {
      BusId = busId;
      Callers = callers;
      Signed = signed;
      Joined = new ConcurrentDictionary<string, SignedCallChain>();
    }

    internal ConcurrentDictionary<string, SignedCallChain> Joined { get; private set; }

    internal SignedCallChain Signed { get; private set; }

    public string BusId { get; private set; }

    public LoginInfo[] Callers { get; private set; }
  }
}
