using System;
using tecgraf.openbus.exceptions;
using tecgraf.openbus.interop.simple;

namespace tecgraf.openbus.test {
  internal class HelloMock : MarshalByRefObject, Hello {
    private readonly Connection _conn;

    internal HelloMock(Connection conn) {
      _conn = conn;
    }

    public void sayHello() {
      CallerChain chain = _conn.CallerChain;
      if (chain == null) {
        throw new NullReferenceException();
      }
      if ((chain.BusId != _conn.BusId) ||
          (!chain.Caller.id.Equals(_conn.Login.Value.id)) ||
          (chain.Originators.Length != 0)) {
        throw new OpenBusInternalException();
      }
    }
  }
}