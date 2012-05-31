using System;
using tecgraf.openbus.exceptions;

namespace tecgraf.openbus.demo.chainvalidation
{
  /// <summary>
  /// Implementação do servant Message.
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
          Console.WriteLine("Uma mensagem foi ignorada pois não veio da secretária.");
          throw new Unavailable();
        }
        string caller = chain.Callers[0].entity;
        string originalCaller = chain.Callers[chain.Callers.Length - 1].entity;
        Console.WriteLine(String.Format("Mensagem recebida de {0} em nome de {1}: {2}", caller, originalCaller, message));
      }
      catch (OpenBusException e) {
        Console.WriteLine("Erro no método sendMessage ao obter a cadeia de chamadas:");
        Console.WriteLine(e);
      }
    }
    #endregion
  }
}
