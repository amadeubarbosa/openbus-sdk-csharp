using tecgraf.openbus.core.v2_00.services.access_control;

namespace tecgraf.openbus.sdk
{
  class CallerChainImpl : CallerChain {

    internal CallerChainImpl(string busId, LoginInfo[] callers) {
      BusId = busId;
      Callers = callers;
    }

    public string BusId { get; private set; }

    public LoginInfo[] Callers { get; private set; }
  }
}
