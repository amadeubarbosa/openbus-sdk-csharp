using tecgraf.openbus;
using tecgraf.openbus.core.v2_0.services.access_control;

namespace Client {
  internal class IndependentClockClientInvalidLoginCallback :
    InvalidLoginCallback {
    public bool InvalidLogin(Connection conn, LoginInfo login, string busId)
    {
      // sempre retorna falso para não prender a chamada atual, pois o cliente
      // deve continuar funcionando mesmo que o barramento esteja fora do ar.
      // Nesta demo, outra parte do código se responsabiliza por relançar a 
      // tentativa de conexão. Essa callback poderia nem ter sido incluída,
      // mas outra opção seria colocar aqui o lançamento de uma thread que
      // tentasse refazer a conexão.
      return false;
    }
  }
}