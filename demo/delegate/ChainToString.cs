  using System;
  using tecgraf.openbus.sdk;

static class ChainToString {
    public static string ToString(CallerChain chain) {
      string ret = String.Empty;
      for (int i = 0; i < chain.Callers.Length; i++) {
        ret += chain.Callers[i].entity + ":";
      }
      return ret.Substring(0, ret.Length - 1);
    }

  }
