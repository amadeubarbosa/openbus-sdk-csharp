using tecgraf.openbus.core.v1_05.access_control_service;
using tecgraf.openbus.core.v2_0.credential;

namespace tecgraf.openbus {
  internal class AnyCredential {
    public CredentialData Credential;
    public Credential LegacyCredential;
    public readonly bool IsLegacy;

    public AnyCredential(CredentialData credential) {
      IsLegacy = false;
      Credential = credential;
    }

    public AnyCredential(Credential credential) {
      IsLegacy = true;
      LegacyCredential = credential;
    }
  }
}
