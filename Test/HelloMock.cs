using System;
using tecgraf.openbus.interop.simple;

namespace tecgraf.openbus.test {
  internal class HelloMock : MarshalByRefObject, Hello {
    public string sayHello() {
      CallerChain chain = ORBInitializer.Context.CallerChain;
      if (chain == null) {
        // cliente deve receber um CORBA::Unknown
        throw new NullReferenceException();
      }
      Connection conn = ORBInitializer.Context.GetCurrentConnection();
      if ((chain.BusId != conn.BusId) ||
          (!chain.Caller.id.Equals(conn.Login.Value.id)) ||
          (chain.Originators.Length != 0) ||
          (!chain.Target.Value.id.Equals(conn.Login.Value.id))) {
        // cliente deve receber um CORBA::Unknown
        throw new InvalidOperationException();
      }
      return "";
    }
  }
}