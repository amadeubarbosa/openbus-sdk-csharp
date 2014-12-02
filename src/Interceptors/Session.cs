namespace tecgraf.openbus.interceptors {
  internal abstract class Session {
    protected Session(int id, byte[] secret, string remoteLogin) {
      Id = id;
      Secret = secret;
      RemoteLogin = remoteLogin;
    }

    public string RemoteLogin { get; private set; }

    public byte[] Secret { get; private set; }

    public int Id { get; private set; }
  }

  internal class ServerSideSession : Session {
    private TicketsHistory Ticket { get; set; }

    public ServerSideSession(int id, byte[] secret, string remoteLogin)
      : base(id, secret, remoteLogin) {
      Ticket = new TicketsHistory();
    }

    public bool CheckTicket(int ticket) {
      lock(Ticket) {
        return Ticket.Check(ticket);
      }
    }

    public string GetTicketHistoryAsString() {
      lock (Ticket) {
        return Ticket.ToString();
      }
    }
  }

  internal class ClientSideSession : Session {
    public ClientSideSession(int id, byte[] secret, string remoteLogin, string entity)
      : base(id, secret, remoteLogin){
      Entity = entity;
      Ticket = 0;
    }

    public string Entity { get; set; }
    public int Ticket { get; set; }
  }
}