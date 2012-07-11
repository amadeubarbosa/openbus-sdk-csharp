using tecgraf.openbus.core.v2_0.services.access_control;

namespace tecgraf.openbus {

  public interface InvalidLoginCallback {

    void InvalidLogin(Connection conn, LoginInfo login, string busid);
  }
}
