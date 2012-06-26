using System;
using tecgraf.openbus.core.v2_00.services.access_control;

namespace tecgraf.openbus.Test {
  class InvalidLoginCallbackMock : InvalidLoginCallback {
    public bool InvalidLogin(Connection conn, LoginInfo login, string busId) {
      throw new NotImplementedException();
    }
  }
}
