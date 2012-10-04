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
    void RegisterService(IComponent component, ServiceProperty[] properties);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="component"></param>
    /// <returns></returns>
    ServiceProperty[] UnregisterService(IComponent component);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="properties"></param>
    /// <param name="retries"></param>
    /// <returns></returns>
    ServiceOfferDesc[] FindServices(ServiceProperty[] properties, int retries);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="retries"></param>
    /// <returns></returns>
    ServiceOfferDesc[] GetAllServices(int retries);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="observer"></param>
    /// <param name="properties"></param>
    void SubscribeObserver(OfferRegistrationObserver observer,
                           ServiceProperty[] properties);

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
