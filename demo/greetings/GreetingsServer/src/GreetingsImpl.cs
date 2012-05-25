using System;

namespace tecgraf.openbus.demo.greetings
{
  /// <summary>
  /// Implementação do servant Greetings.
  /// </summary>  
  public class GreetingsImpl : MarshalByRefObject, Greetings
  {
    #region Fields

    private readonly Connection _conn;
    private readonly Languages _language;
    private readonly Period _period;

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

    public GreetingsImpl(Connection conn, Languages language, Period period) {
      _conn = conn;
      _language = language;
      _period = period;
    }

    #endregion

    #region Greetings Members

    public string sayGreetings() {
      if (_language.Equals(Languages.English)) {
        return EnglishGreetings();
      }
      return _language.Equals(Languages.Spanish) ? SpanishGreetings() : PortugueseGreetings();
    }
    #endregion

    private string EnglishGreetings() {
      string caller = _conn.CallerChain.Callers[0].entity;
      if (_period.Equals(Period.Morning)) {
        return String.Format("Good morning {0}!", caller);
      }
      if (_period.Equals(Period.Afternoon)) {
        return String.Format("Good afternoon {0}!", caller);
      }
      return String.Format("Good night {0}!", caller);
    }

    private string SpanishGreetings() {
      string caller = _conn.CallerChain.Callers[0].entity;
      if (_period.Equals(Period.Morning)) {
        return String.Format("Buenos días {0}!", caller);
      }
      if (_period.Equals(Period.Afternoon)) {
        return String.Format("Buenas tardes {0}!", caller);
      }
      return String.Format("Buenas noches {0}!", caller);
    }

    private string PortugueseGreetings() {
      string caller = _conn.CallerChain.Callers[0].entity;
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
