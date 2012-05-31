using System;

namespace tecgraf.openbus.interop.multiplexing {
  class HelloInvalidLoginCallback : InvalidLoginCallback {
    private readonly string _login;
    private readonly System.Text.UTF8Encoding _encoding;
    private readonly ConnectionManager _manager;

    public HelloInvalidLoginCallback(string login, ConnectionManager manager) {
      _login = login;
      _encoding = new System.Text.UTF8Encoding();
      _manager = manager;
    }

    public bool InvalidLogin(Connection conn) {
      try {
        Console.WriteLine("Callback de InvalidLogin da conexão " + _login + " foi chamada, tentando logar novamente no barramento.");
        _manager.Requester = conn;
        conn.LoginByPassword(_login, _encoding.GetBytes(_login));
        return conn.Login != null;
      }
      catch (Exception e) {
        Console.WriteLine(e.StackTrace);
      }
      return false;
    }
  }
}
