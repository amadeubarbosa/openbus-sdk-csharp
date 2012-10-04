using System;
using scs.core;
using tecgraf.openbus.core.v2_0.services.offer_registry;

namespace tecgraf.openbus.assistant.callbacks {
  internal class AssistantOnFailureCallback : OnFailureCallback {
    public void OnLoginFailure(Assistant assistant, Exception e) {
      // não faz nada
    }

    public void OnRegisterFailure(Assistant assistant, IComponent component,
                                  ServiceProperty[] props, Exception e) {
      // não faz nada
    }

    public void OnFindFailure(Assistant assistant, Exception e) {
      // não faz nada
    }
  }
}
