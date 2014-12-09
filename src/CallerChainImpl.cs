using System;
using Ch.Elca.Iiop.Idl;
using omg.org.CORBA;
using tecgraf.openbus.caches;
using tecgraf.openbus.core.v2_0.credential;
using tecgraf.openbus.core.v2_1.credential;
using tecgraf.openbus.core.v2_1.services.access_control;
using tecgraf.openbus.interceptors;
using TypeCode = omg.org.CORBA.TypeCode;

namespace tecgraf.openbus {
  internal class CallerChainImpl : CallerChain {
    internal CallerChainImpl(AnyCredential anyCredential) {
      Legacy = anyCredential.Legacy;
      if (Legacy) {
        core.v2_0.services.access_control.CallChain legacyChain = UnmarshalLegacyCallChain(anyCredential.LegacyChain);
        Target = legacyChain.target;
        Originators = new LoginInfo[legacyChain.originators.Length];
        legacyChain.originators.CopyTo(Originators, 0);
        Caller = new LoginInfo(legacyChain.caller.id, legacyChain.caller.entity);
        Signed = new AnySignedChain { LegacyChain = anyCredential.LegacyChain };
        Signed.Encoded = Signed.LegacyChain.encoded;
        Signed.Signature = Signed.LegacyChain.signature;
      }
      else {
        CallChain chain = UnmarshalCallChain(anyCredential.Chain);
        BusId = chain.bus;
        Target = chain.target;
        Originators = chain.originators;
        Caller = chain.caller;
        Signed = new AnySignedChain { Chain = anyCredential.Chain };
        Signed.Encoded = Signed.Chain.encoded;
        Signed.Signature = Signed.Chain.signature;
      }
      Joined = new LRUConcurrentDictionaryCache<string, AnySignedChain>();
    }

    #region Public Members

    public string BusId { get; private set; }

    public string Target { get; private set; }

    public LoginInfo[] Originators { get; private set; }

    public LoginInfo Caller { get; private set; }

    #endregion

    #region Internal Members

    internal bool Legacy { get; private set; }

    internal LRUConcurrentDictionaryCache<string, AnySignedChain> Joined { get; private set; }

    internal AnySignedChain Signed { get; private set; }

    internal static CallChain UnmarshalCallChain(SignedData signed) {
      Type chainType = typeof(CallChain);
      TypeCode chainTypeCode =
        OrbServices.GetSingleton().create_interface_tc(Repository.GetRepositoryID(chainType),
                                chainType.Name);
      return (CallChain)InterceptorsInitializer.Codec.decode_value(signed.encoded, chainTypeCode);
    }

    private static core.v2_0.services.access_control.CallChain UnmarshalLegacyCallChain(SignedCallChain signed) {
      Type chainType = typeof(core.v2_0.services.access_control.CallChain);
      TypeCode chainTypeCode =
        OrbServices.GetSingleton().create_interface_tc(Repository.GetRepositoryID(chainType),
                                chainType.Name);
      return (core.v2_0.services.access_control.CallChain)InterceptorsInitializer.Codec.decode_value(signed.encoded, chainTypeCode);
    }

    #endregion
  }

  internal class AnySignedChain {
    public SignedCallChain LegacyChain;
    public SignedData Chain;
    public byte[] Signature;
    public byte[] Encoded;
  }
}