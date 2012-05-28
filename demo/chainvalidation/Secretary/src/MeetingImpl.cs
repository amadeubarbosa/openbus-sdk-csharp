using System;
using tecgraf.openbus.exceptions;

namespace tecgraf.openbus.demo.chainvalidation
{
  /// <summary>
  /// Implementação do servant Meeting.
  /// </summary>  
  public class MeetingImpl : MarshalByRefObject, Meeting
  {
    #region Fields

    private readonly Connection _conn;
    private readonly Message _executive;
    private int _hour;

    #endregion

    #region Constructors

    public MeetingImpl(Connection conn, Message executive) {
      _conn = conn;
      _executive = executive;
      _hour = 8;
    }

    #endregion

    #region Meeting Members

    public int bookMeeting() {
      try {
        CallerChain chain = _conn.CallerChain;
        if (chain == null) {
          Console.WriteLine("A cadeia de chamadas é nula, talvez o serviço não esteja logado no barramento. Impossível descobrir quem fez a chamada.");
          return -1;
        }
        string caller = chain.Callers[0].entity;
        Console.WriteLine(String.Format("Pedido de reunião recebido de {0}.", caller));
        _executive.sendMessage(String.Format("Você tem uma reunião às {0}h com {1}.", _hour, caller));
        int ret = _hour;
        if (_hour >= 18) {
          _hour = 8;
        }
        else {
          _hour++;
        }
        return ret;
      }
      catch (OpenBusException e) {
        Console.WriteLine("Erro no método bookMeeting ao obter a cadeia de chamadas:");
        Console.WriteLine(e.StackTrace);
      }
      return -1;
    }
    #endregion
  }
}
