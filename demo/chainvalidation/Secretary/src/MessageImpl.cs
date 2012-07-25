using System;
using tecgraf.openbus;
using tecgraf.openbus.exceptions;

namespace chainvalidation {
  /// <summary>
  /// Implementa��o do servant Message.
  /// </summary>  
  public class MessageImpl : MarshalByRefObject, Message {
    #region Fields

    private readonly Connection _dispatcherConn;

    #endregion

    #region Constructors

    internal MessageImpl(Connection dispatcherConn) {
      _dispatcherConn = dispatcherConn;
    }

    #endregion

    #region Message Members

    public void sendMessage(string message) {
      try {
        Console.WriteLine(String.Format("Mensagem recebida de {0}: {1}",
                                        _dispatcherConn.CallerChain.Caller.
                                          entity, message));
      }
      catch (OpenBusException e) {
        Console.WriteLine(
          "Erro no m�todo sendMessage ao obter a cadeia de chamadas:");
        Console.WriteLine(e);
      }
    }

    #endregion
  }
}