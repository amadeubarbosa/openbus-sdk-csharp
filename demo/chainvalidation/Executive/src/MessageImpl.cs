using System;
using tecgraf.openbus.exceptions;

namespace tecgraf.openbus.demo.chainvalidation
{
  /// <summary>
  /// Implementa��o do servant Message.
  /// </summary>  
  public class MessageImpl : MarshalByRefObject, Message
  {
    #region Fields

    private readonly Connection _conn;
    private readonly string _secretaryEntity;

    #endregion

    #region Constructors

    public MessageImpl(Connection conn, string secretaryEntity) {
      _conn = conn;
      _secretaryEntity = secretaryEntity;
    }

    #endregion

    #region Message Members

    public void sendMessage(string message) {
      try {
        CallerChain chain = _conn.CallerChain;
        if ((chain == null) || (!chain.Callers[0].entity.Equals(_secretaryEntity))) {
          Console.WriteLine("Uma mensagem foi ignorada pois n�o veio da secret�ria.");
          throw new Unavailable();
        }
        Console.WriteLine(String.Format("Mensagem recebida de {0}: {1}", chain.Callers[0].entity, message));
      }
      catch (OpenBusException e) {
        Console.WriteLine("Erro no m�todo sendMessage ao obter a cadeia de chamadas:");
        Console.WriteLine(e.StackTrace);
      }
    }
    #endregion
  }
}
