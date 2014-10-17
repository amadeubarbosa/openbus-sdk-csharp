using tecgraf.openbus.caches;
using tecgraf.openbus.core.v2_1.services.access_control;
using tecgraf.openbus.core.v2_1.credential;

namespace tecgraf.openbus {
  internal class CallerChainImpl : CallerChain {
    internal static readonly SignedData NullSignedCallChain = new SignedData(new byte[256], new byte[0]);

    internal CallerChainImpl(string busId, LoginInfo caller,
                             string target, LoginInfo[] originators,
                             SignedData signed)
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
      Joined = new LRUConcurrentDictionaryCache<string, SignedData>();
    }

    internal LRUConcurrentDictionaryCache<string, SignedData> Joined { get; private set; }

    internal SignedData Signed { get; private set; }

    public string BusId { get; private set; }

    public string Target { get; private set; }

    public LoginInfo[] Originators { get; private set; }

    public LoginInfo Caller { get; private set; }
  }
}