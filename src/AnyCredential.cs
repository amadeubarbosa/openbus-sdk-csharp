using System;
using Ch.Elca.Iiop.Idl;
using omg.org.CORBA;
using omg.org.IOP;
using tecgraf.openbus.core.v2_0.credential;
using tecgraf.openbus.core.v2_1.credential;
using tecgraf.openbus.interceptors;
using CredentialData = tecgraf.openbus.core.v2_1.credential.CredentialData;
using TypeCode = omg.org.CORBA.TypeCode;

namespace tecgraf.openbus {
  internal class AnyCredential {
    public readonly bool Legacy;
    public readonly string Bus;
    public readonly string Login;
    public readonly int Session;
    public readonly int Ticket;
    public readonly byte[] Hash;
    public SignedData Chain;
    public SignedCallChain LegacyChain;

    public AnyCredential(ServiceContext serviceContext, bool legacyContext) {
      if (legacyContext) {
        core.v2_0.credential.CredentialData credential =
          UnmarshalLegacyCredential(serviceContext);
        Legacy = true;
        Bus = credential.bus;
        Login = credential.login;
        Session = credential.session;
        Ticket = credential.ticket;
        Hash = credential.hash;
        LegacyChain = credential.chain;
      }
      else {
        CredentialData credential = UnmarshalCredential(serviceContext);
        Legacy = false;
        Bus = credential.bus;
        Login = credential.login;
        Session = credential.session;
        Ticket = credential.ticket;
        Hash = credential.hash;
        Chain = credential.chain;
      }
    }

    private static CredentialData UnmarshalCredential(ServiceContext serviceContext) {
      OrbServices orb = OrbServices.GetSingleton();
      TypeCode credentialTypeCode =
        orb.create_tc_for_type(typeof (CredentialData));

      byte[] data = serviceContext.context_data;
      return (CredentialData) InterceptorsInitializer.Codec.decode_value(data, credentialTypeCode);
    }

    private static core.v2_0.credential.CredentialData UnmarshalLegacyCredential(
      ServiceContext serviceContext) {
      OrbServices orb = OrbServices.GetSingleton();
      TypeCode credentialTypeCode =
        orb.create_tc_for_type(typeof(core.v2_0.credential.CredentialData));
      byte[] data = serviceContext.context_data;
      return
        (core.v2_0.credential.CredentialData)
          InterceptorsInitializer.Codec.decode_value(data, credentialTypeCode);
    }
  }
}
