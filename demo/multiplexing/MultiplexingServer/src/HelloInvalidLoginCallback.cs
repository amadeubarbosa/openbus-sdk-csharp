using System;
using tecgraf.openbus.sdk;

namespace MultiplexingServer {
  class HelloInvalidLoginCallback : InvalidLoginCallback {
    private readonly string _login;
    private readonly System.Text.UTF8Encoding _encoding;

    public HelloInvalidLoginCallback(string login) {
      _login = login;
      _encoding = new System.Text.UTF8Encoding();
    }

    public bool InvalidLogin(Connection conn) {
      try {
        Console.WriteLine("Callback de InvalidLogin da conexão " + _login + " foi chamada, tentando logar novamente no barramento.");
        conn.LoginByCertificate(_login, _encoding.GetBytes(_login));
        return conn.Login != null;
      }
      catch (Exception e) {
        Console.WriteLine(e.StackTrace);
      }
      return false;
    }
  }
}
