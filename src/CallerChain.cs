using tecgraf.openbus.core.v2_00.services.access_control;

namespace tecgraf.openbus {
  
  public interface CallerChain {

    string BusId { get; }

    LoginInfo[] Callers { get; }
  }
}
