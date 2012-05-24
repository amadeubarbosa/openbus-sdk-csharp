using System;

namespace tecgraf.openbus.Test {
  class InvalidLoginCallbackMock : InvalidLoginCallback {
    public bool InvalidLogin(Connection conn) {
      throw new NotImplementedException();
    }
  }
}
