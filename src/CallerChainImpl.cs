using omg.org.CORBA;
using tecgraf.openbus.caches;
using tecgraf.openbus.core.v2_0.credential;
using tecgraf.openbus.core.v2_1.credential;
using tecgraf.openbus.core.v2_1.services.access_control;
using tecgraf.openbus.interceptors;
using TypeCode = omg.org.CORBA.TypeCode;

namespace tecgraf.openbus {
  internal class CallerChainImpl : CallerChain {
    private CallerChainImpl() {
      Joined = new LRUConcurrentDictionaryCache<string, AnySignedChain>();
    }

    internal CallerChainImpl(string busId, string target) : this() {
      BusId = busId;
      Target = target;
    }

    internal CallerChainImpl(string busId,
      core.v2_0.services.access_control.LoginInfo caller, string target,
      core.v2_0.services.access_control.LoginInfo[] originators,
      SignedCallChain signed)
      : this(busId, target) {
      Originators = new LoginInfo[originators.Length];
      for (int i = 0; i < originators.Length; i++) {
        Originators[i] = new LoginInfo(originators[i].id, originators[i].entity);
      }
      Caller = new LoginInfo(caller.id, caller.entity);
      Signed = new AnySignedChain(signed);
      Legacy = true;
    }

    internal CallerChainImpl(string busId, LoginInfo caller, string target,
      LoginInfo[] originators, SignedData signed)
      : this(busId, target) {
      Originators = originators;
      Caller = caller;
      Signed = new AnySignedChain(signed);
      Legacy = false;
    }

    internal CallerChainImpl(string busId, LoginInfo caller, string target,
      LoginInfo[] originators, SignedData signed, SignedCallChain legacySigned)
      : this(busId, target) {
      Originators = originators;
      Caller = caller;
      Signed = new AnySignedChain(signed, legacySigned);
      Legacy = false;
    }

    internal CallerChainImpl(AnyCredential anyCredential)
      : this() {
      Legacy = anyCredential.Legacy;
      if (Legacy) {
        core.v2_0.services.access_control.CallChain legacyChain = UnmarshalLegacyCallChain(anyCredential.LegacyChain);
        BusId = anyCredential.Bus;
        Target = legacyChain.target;
        Originators = new LoginInfo[legacyChain.originators.Length];
        for (int i = 0; i < legacyChain.originators.Length; i++) {
          Originators[i] = new LoginInfo(legacyChain.originators[i].id, legacyChain.originators[i].entity);
        }
        Caller = new LoginInfo(legacyChain.caller.id, legacyChain.caller.entity);
        Signed = new AnySignedChain(anyCredential.LegacyChain);
      }
      else {
        CallChain chain = UnmarshalCallChain(anyCredential.Chain);
        BusId = chain.bus;
        Target = chain.target;
        Originators = chain.originators;
        Caller = chain.caller;
        Signed = new AnySignedChain(anyCredential.Chain);
      }
    }

    #region Public Members

    public string BusId { get; private set; }

    public string Target { get; private set; }

    public LoginInfo[] Originators { get; private set; }

    public LoginInfo Caller { get; private set; }

    #endregion

    #region Internal Members

    internal bool Legacy { get; set; }

    internal LRUConcurrentDictionaryCache<string, AnySignedChain> Joined { get; private set; }

    internal AnySignedChain Signed { get; private set; }

    internal static CallChain UnmarshalCallChain(SignedData signed) {
      TypeCode chainTypeCode =
        OrbServices.GetSingleton().create_tc_for_type(typeof(CallChain));
      return (CallChain)InterceptorsInitializer.Codec.decode_value(signed.encoded, chainTypeCode);
    }

    internal static core.v2_0.services.access_control.CallChain UnmarshalLegacyCallChain(SignedCallChain signed) {
      TypeCode chainTypeCode =
        OrbServices.GetSingleton().create_tc_for_type(typeof(core.v2_0.services.access_control.CallChain));
      return (core.v2_0.services.access_control.CallChain)InterceptorsInitializer.Codec.decode_value(signed.encoded, chainTypeCode);
    }

    #endregion
  }

  internal class AnySignedChain {
    public SignedCallChain LegacyChain;
    public SignedData Chain;
    public readonly byte[] Signature;
    public readonly byte[] Encoded;

    public AnySignedChain(SignedData signed) {
      Signature = signed.signature;
      Encoded = signed.encoded;
      Chain = signed;
    }

    public AnySignedChain(SignedCallChain signed) {
      Signature = signed.signature;
      Encoded = signed.encoded;
      LegacyChain = signed;
    }

    public AnySignedChain(SignedData signed, SignedCallChain legacy)
      : this(signed) {
      LegacyChain = legacy;
    }
  }
}