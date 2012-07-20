using System;
using tecgraf.openbus;
using tecgraf.openbus.exceptions;

namespace chainvalidation {
  /// <summary>
  /// Implementação do servant Message.
  /// </summary>  
  public class MessageImpl : MarshalByRefObject, Message {
    #region Fields

    private readonly Connection _conn;
    private readonly string _secretaryEntity;

    #endregion

    #region Constructors

    internal MessageImpl(Connection conn, string secretaryEntity) {
      _conn = conn;
      _secretaryEntity = secretaryEntity;
    }

    #endregion

    #region Message Members

    public void sendMessage(string message) {
      try {
        CallerChain chain = _conn.CallerChain;
        if (!chain.Caller.entity.Equals(_secretaryEntity)) {
          Console.WriteLine(
            String.Format(
              "Uma mensagem foi ignorada pois não veio da secretária {0}.",
              _secretaryEntity));
          throw new Unavailable();
        }
        string originalCaller = chain.Originators[0].entity;
        string caller = chain.Caller.entity;
        Console.WriteLine(
          String.Format("Mensagem recebida de {0} em nome de {1}: {2}", caller,
                        originalCaller, message));
      }
      catch (OpenBusException e) {
        Console.WriteLine(
          "Erro no método sendMessage ao obter a cadeia de chamadas:");
        Console.WriteLine(e);
      }
    }

    #endregion
  }
}