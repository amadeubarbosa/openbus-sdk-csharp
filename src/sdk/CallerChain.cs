﻿using tecgraf.openbus.core.v2_00.services.access_control;

namespace tecgraf.openbus.sdk {
  
  public interface CallerChain {

    string BusId();

    LoginInfo[] Callers();
  }
}
