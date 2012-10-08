using System.Collections.Generic;
using System.Linq;
using tecgraf.openbus;

namespace demo {
  internal class MultiplexingCallDispatchCallback : CallDispatchCallback {
    private readonly Dictionary<string, Connection> _connections;

    internal MultiplexingCallDispatchCallback(
      Dictionary<string, Connection> connections) {
      _connections = connections;
    }

    public Connection Dispatch(OpenBusContext context, string busid,
                               string caller, string uri, string operation) {
      // tenta obter a conexão associada a esse servant. Se não conseguir, retorna qualquer uma disponível.
      lock (_connections) {
        return _connections.ContainsKey(uri)
                 ? _connections[uri]
                 : _connections.Values.FirstOrDefault();
      }
    }
  }
}