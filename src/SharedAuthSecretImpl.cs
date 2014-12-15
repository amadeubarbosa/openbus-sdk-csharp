using tecgraf.openbus.core.v2_1.services.access_control;

namespace tecgraf.openbus {
  internal class SharedAuthSecretImpl : SharedAuthSecret {
    private readonly OpenBusContextImpl _context;

    internal SharedAuthSecretImpl(string busId, LoginProcess attempt, byte[] secret, core.v2_0.services.access_control.LoginProcess legacyAttempt) {
      BusId = busId;
      Attempt = attempt;
      LegacyAttempt = legacyAttempt;
      Secret = secret;
      Legacy = attempt == null;
      _context = (OpenBusContextImpl) ORBInitializer.Context;
    }

    public string BusId { get; private set; }
    
    public void Cancel() {
      _context.IgnoreCurrentThread();
      try {
        Attempt.cancel();
      }
      finally {
        _context.UnignoreCurrentThread();
      }
    }

    internal LoginProcess Attempt { get; set; }
    internal core.v2_0.services.access_control.LoginProcess LegacyAttempt { get; set; }
    internal byte[] Secret { get; set; }
    internal bool Legacy { get; private set; }
  }
}
