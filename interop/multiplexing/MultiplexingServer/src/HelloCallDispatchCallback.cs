namespace tecgraf.openbus.interop.multiplexing {
  class HelloCallDispatchCallback : CallDispatchCallback {
    private readonly Connection _connAtBus1;
    private readonly Connection _connAtBus2;

    public HelloCallDispatchCallback(Connection connAtBus1, Connection connAtBus2) {
      _connAtBus1 = connAtBus1;
      _connAtBus2 = connAtBus2;
    }

    public Connection Dispatch(OpenBusContext context, string busid,
                               string loginId, byte[] objectId, string operation) {
      if (busid.Equals(_connAtBus1.BusId)) {
        return _connAtBus1;
      }
      return busid.Equals(_connAtBus2.BusId) ? _connAtBus2 : null;
    }
  }
}
