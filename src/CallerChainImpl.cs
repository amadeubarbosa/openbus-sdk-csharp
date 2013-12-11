using tecgraf.openbus.caches;
using tecgraf.openbus.core.v2_0.services.access_control;
using tecgraf.openbus.core.v2_0.credential;

namespace tecgraf.openbus {
  internal class CallerChainImpl : CallerChain {
    internal static readonly SignedCallChain NullSignedCallChain = new SignedCallChain(new byte[256], new byte[0]);

    internal CallerChainImpl(string busId, LoginInfo caller,
                             string target, LoginInfo[] originators,
                             SignedCallChain signed)
      : this(busId, caller, target, originators) {
      Signed = signed;
    }

    internal CallerChainImpl(string busId, LoginInfo caller, string target,
                             LoginInfo[] originators) {
      BusId = busId;
      Caller = caller;
      Target = target;
      Originators = originators;
      Signed = NullSignedCallChain;
      Joined = new LRUConcurrentDictionaryCache<string, SignedCallChain>();
    }

    internal LRUConcurrentDictionaryCache<string, SignedCallChain> Joined { get; private set; }

    internal SignedCallChain Signed { get; private set; }

    public string BusId { get; private set; }

    public string Target { get; private set; }

    public LoginInfo[] Originators { get; private set; }

    public LoginInfo Caller { get; private set; }
  }
}