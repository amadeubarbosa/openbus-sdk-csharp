using System.Collections;
using System.Collections.Generic;
using omg.org.IOP;

namespace tecgraf.openbus.interceptors {
  internal class EffectiveProfile {
    private readonly byte[] _profileData;

    public EffectiveProfile(TaggedProfile effectiveProfile) {
      _profileData = effectiveProfile.profile_data;
    }

    public override bool Equals(object obj) {
      if (ReferenceEquals(null, obj)) {
        return false;
      }
      if (ReferenceEquals(this, obj)) {
        return true;
      }
      EffectiveProfile prof = obj as EffectiveProfile;
      if (prof != null) {
        IStructuralEquatable eqProfile = prof._profileData;
        return
          (eqProfile.Equals(_profileData,
                            StructuralComparisons.StructuralEqualityComparer));
      }
      return false;
    }

    public override int GetHashCode() {
      return (_profileData != null
                ? ((IStructuralEquatable) _profileData).GetHashCode(
                  EqualityComparer<object>.Default)
                : 0);
    }
  }
}