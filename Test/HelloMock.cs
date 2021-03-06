﻿using System;
using tecgraf.openbus.interop.simple;

namespace tecgraf.openbus.test {
  public class HelloMock : MarshalByRefObject, Hello {
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
          (!chain.Target.Equals(conn.Login.Value.entity))) {
        // cliente deve receber um CORBA::Unknown
        throw new InvalidOperationException();
      }
      return "";
    }

    public override object InitializeLifetimeService() {
      return null;
    }
  }
}