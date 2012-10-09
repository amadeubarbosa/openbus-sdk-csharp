namespace tecgraf.openbus.test {
  public class CallDispatchCallbackImpl {
    private readonly Connection _conn;

    public CallDispatchCallbackImpl(Connection conn) {
      _conn = conn;
    }

    public Connection Dispatch(OpenBusContext context, string busid,
                               string caller, string uri, string operation) {
      return _conn.BusId.Equals(busid) ? _conn : null;
    }
  }
}