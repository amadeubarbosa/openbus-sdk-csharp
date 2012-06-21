using System;

namespace tecgraf.openbus.interop.utils {
  public static class ChainToString {
    public static string ToString(CallerChain chain) {
      string ret = String.Empty;
      for (int i = 0; i < chain.Originators.Length; i++) {
        ret += chain.Originators[i].entity + "->";
      }
      return ret + chain.Caller.entity;
    }
  }
}
