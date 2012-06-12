using System;
using tecgraf.openbus;
using tecgraf.openbus.core.v2_00.services.access_control;

namespace multiplexing {
  /// <summary>
  /// Implementação do servant Greetings.
  /// </summary>  
  public class GreetingsImpl : MarshalByRefObject, Greetings {
    #region Fields

    private readonly Languages _language;
    private readonly Period _period;
    private string _busId;

    public enum Languages {
      English,
      Spanish,
      Portuguese
    }

    public enum Period {
      Morning,
      Afternoon,
      Night
    }

    #endregion

    #region Constructors

    public GreetingsImpl(string busId, Languages language, Period period) {
      _busId = busId;
      _language = language;
      _period = period;
    }

    #endregion

    #region Greetings Members

    public string sayGreetings() {
      if (_language.Equals(Languages.English)) {
        return EnglishGreetings();
      }
      return _language.Equals(Languages.Spanish)
               ? SpanishGreetings()
               : PortugueseGreetings();
    }

    #endregion

    private string EnglishGreetings() {
      Connection conn = ORBInitializer.Manager.GetDispatcher(_busId);
      LoginInfo[] callers = conn.CallerChain.Callers;
      string caller = conn.CallerChain.Callers[callers.Length - 1].entity;
      if (_period.Equals(Period.Morning)) {
        return String.Format("Good morning {0}!", caller);
      }
      if (_period.Equals(Period.Afternoon)) {
        return String.Format("Good afternoon {0}!", caller);
      }
      return String.Format("Good night {0}!", caller);
    }

    private string SpanishGreetings() {
      Connection conn = ORBInitializer.Manager.GetDispatcher(_busId);
      LoginInfo[] callers = conn.CallerChain.Callers;
      string caller = conn.CallerChain.Callers[callers.Length - 1].entity;
      if (_period.Equals(Period.Morning)) {
        return String.Format("Buenos días {0}!", caller);
      }
      if (_period.Equals(Period.Afternoon)) {
        return String.Format("Buenas tardes {0}!", caller);
      }
      return String.Format("Buenas noches {0}!", caller);
    }

    private string PortugueseGreetings() {
      Connection conn = ORBInitializer.Manager.GetDispatcher(_busId);
      LoginInfo[] callers = conn.CallerChain.Callers;
      string caller = conn.CallerChain.Callers[callers.Length - 1].entity;
      if (_period.Equals(Period.Morning)) {
        return String.Format("Bom dia {0}!", caller);
      }
      if (_period.Equals(Period.Afternoon)) {
        return String.Format("Boa tarde {0}!", caller);
      }
      return String.Format("Boa noite {0}!", caller);
    }
  }
}