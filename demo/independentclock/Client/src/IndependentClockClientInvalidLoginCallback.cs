using tecgraf.openbus;
using tecgraf.openbus.core.v2_0.services.access_control;

namespace Client {
  internal class IndependentClockClientInvalidLoginCallback :
    InvalidLoginCallback {
    public void InvalidLogin(Connection conn, LoginInfo login, string busId) {
      // Nesta demo, outra parte do código se responsabiliza por relançar a 
      // tentativa de conexão. Essa callback poderia nem ter sido incluída,
      // mas outra opção seria colocar aqui o lançamento de uma thread que
      // tentasse refazer a conexão.
    }
  }
}