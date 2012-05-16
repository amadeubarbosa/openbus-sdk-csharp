using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace tecgraf.openbus.Test {
  class InvalidLoginCallbackMock : InvalidLoginCallback {
    public bool InvalidLogin(Connection conn) {
      throw new NotImplementedException();
    }
  }
}
