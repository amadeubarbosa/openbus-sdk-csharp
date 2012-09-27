using omg.org.CORBA;
using scs.core;
using tecgraf.openbus.core.v2_0.services.offer_registry;

namespace tecgraf.openbus.assistant {
  /// <summary>
  /// 
  /// </summary>
  public interface Assistant {
    /// <summary>
    /// 
    /// </summary>
    /// <param name="component"></param>
    /// <param name="properties"></param>
    void AddOffer(IComponent component, ServiceProperty[] properties);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="properties"></param>
    /// <param name="retries"></param>
    /// <returns></returns>
    ServiceOfferDesc[] FindOffers(ServiceProperty[] properties, int retries);

    /// <summary>
    /// 
    /// </summary>
    void Shutdown();

    /// <summary>
    /// 
    /// </summary>
    ORB Orb { get; }
  }
}
