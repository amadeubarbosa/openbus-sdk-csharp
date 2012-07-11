using System;
using tecgraf.openbus.core.v2_0.services.access_control;

namespace tecgraf.openbus.Test {
  class InvalidLoginCallbackMock : InvalidLoginCallback {
    public void InvalidLogin(Connection conn, LoginInfo login, string busId) {
      throw new NotImplementedException();
    }
  }
}
