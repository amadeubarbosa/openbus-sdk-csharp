using tecgraf.openbus.core.v2_00.services.access_control;

namespace tecgraf.openbus {

  public interface InvalidLoginCallback {

    bool InvalidLogin(Connection conn, LoginInfo login, string busid);
  }
}
