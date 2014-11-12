using tecgraf.openbus.core.v2_0.services.access_control;

namespace tecgraf.openbus {
  internal class SharedAuthSecretImpl : SharedAuthSecret {
    private readonly OpenBusContextImpl _context;

    internal SharedAuthSecretImpl(string busId, LoginProcess attempt, byte[] secret) {
      BusId = busId;
      Attempt = attempt;
      Secret = secret;
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
    internal byte[] Secret { get; set; }
  }
}
