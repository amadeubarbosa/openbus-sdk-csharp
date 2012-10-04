using System;
using scs.core;
using tecgraf.openbus.core.v2_0.services.offer_registry;

namespace tecgraf.openbus.assistant {
  /// <summary>
  /// 
  /// </summary>
  public interface OnFailureCallback {
    /// <summary>
    /// 
    /// </summary>
    /// <param name="assistant"></param>
    /// <param name="e"></param>
    void OnLoginFailure(Assistant assistant, Exception e);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="assistant"></param>
    /// <param name="component"></param>
    /// <param name="props"></param>
    /// <param name="e"></param>
    void OnRegisterFailure(Assistant assistant,
                           IComponent component, 
                           ServiceProperty[] props,
                           Exception e);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="assistant"></param>
    /// <param name="e"></param>
    void OnFindFailure(Assistant assistant, Exception e);
  }
}
