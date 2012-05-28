using System;
using tecgraf.openbus.exceptions;

namespace tecgraf.openbus.demo.chainvalidation
{
  /// <summary>
  /// Implementa��o do servant Meeting.
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
          Console.WriteLine("A cadeia de chamadas � nula, talvez o servi�o n�o esteja logado no barramento. Imposs�vel descobrir quem fez a chamada.");
          return -1;
        }
        string caller = chain.Callers[0].entity;
        Console.WriteLine(String.Format("Pedido de reuni�o recebido de {0}.", caller));
        _executive.sendMessage(String.Format("Voc� tem uma reuni�o �s {0}h com {1}.", _hour, caller));
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
        Console.WriteLine("Erro no m�todo bookMeeting ao obter a cadeia de chamadas:");
        Console.WriteLine(e.StackTrace);
      }
      return -1;
    }
    #endregion
  }
}
