using tecgraf.openbus.caches;
using tecgraf.openbus.core.v2_0.services.access_control;
using tecgraf.openbus.core.v2_0.credential;

namespace tecgraf.openbus {
  internal class CallerChainImpl : CallerChain {
    internal CallerChainImpl(string busId, LoginInfo caller, LoginInfo[] originators,
                             SignedCallChain signed) : this(busId, caller, originators) {
      Signed = signed;
    }

    internal CallerChainImpl(string busId, LoginInfo caller, LoginInfo[] originators) {
      BusId = busId;
      Caller = caller;
      Originators = originators;
      Joined = new LRUConcurrentDictionaryCache<string, SignedCallChain>();
    }

    internal LRUConcurrentDictionaryCache<string, SignedCallChain> Joined { get; private set; }

    internal SignedCallChain Signed { get; private set; }

    public string BusId { get; private set; }

    public LoginInfo[] Originators { get; private set; }

    public LoginInfo Caller { get; private set; }
  }
}