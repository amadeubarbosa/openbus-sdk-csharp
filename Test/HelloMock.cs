using System;
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
        // cliente deve receber um CORBA::Unknown
        throw new NullReferenceException();
      }
      if ((chain.BusId != _conn.BusId) ||
          (!chain.Caller.id.Equals(_conn.Login.Value.id)) ||
          (chain.Originators.Length != 0)) {
        // cliente deve receber um CORBA::Unknown
        throw new InvalidOperationException();
      }
    }
  }
}