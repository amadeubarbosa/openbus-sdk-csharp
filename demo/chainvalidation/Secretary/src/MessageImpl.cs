using System;
using tecgraf.openbus;
using tecgraf.openbus.exceptions;

namespace chainvalidation {
  /// <summary>
  /// Implementação do servant Message.
  /// </summary>  
  internal class MessageImpl : MarshalByRefObject, Message {
    #region Fields

    private readonly Connection _conn;

    #endregion

    #region Constructors

    internal MessageImpl(Connection conn) {
      _conn = conn;
    }

    #endregion

    #region Message Members

    public void sendMessage(string message) {
      try {
        CallerChain chain = _conn.CallerChain;
        if (chain == null) {
          Console.WriteLine(
            "A cadeia de chamadas é nula, talvez o serviço não esteja logado no barramento. Impossível descobrir quem fez a chamada.");
          Console.WriteLine(String.Format("Mensagem recebida: {0}", message));
          return;
        }
        Console.WriteLine(String.Format("Mensagem recebida de {0}: {1}",
                                        chain.Caller.entity, message));
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