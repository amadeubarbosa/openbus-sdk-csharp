using System;
using tecgraf.openbus;

namespace multiplexing {
  /// <summary>
  /// Implementação do servant Greetings.
  /// </summary>  
  public class GreetingsImpl : MarshalByRefObject, Greetings {
    #region Fields

    private readonly Connection _conn;
    private readonly Language _language;
    private readonly Period _period;

    public enum Language {
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

    public GreetingsImpl(Connection conn, Language language, Period period) {
      _conn = conn;
      _language = language;
      _period = period;
    }

    #endregion

    #region Greetings Members

    public string sayGreetings() {
      string caller = _conn.CallerChain.Caller.entity;
      switch (_language) {
        case Language.English:
          return EnglishGreetings(caller);
        case Language.Spanish:
          return SpanishGreetings(caller);
        case Language.Portuguese:
          return PortugueseGreetings(caller);
      }
      return "Erro: língua não especificada.";
    }

    #endregion

    #region Private Members

    private string EnglishGreetings(string caller) {
      switch (_period) {
        case Period.Morning:
          return String.Format("Good morning {0}!", caller);
        case Period.Afternoon:
          return String.Format("Good afternoon {0}!", caller);
        case Period.Night:
          return String.Format("Good night {0}!", caller);
      }
      return "Error: Period not specified.";
    }

    private string SpanishGreetings(string caller) {
      switch (_period) {
        case Period.Morning:
          return String.Format("Buenos días {0}!", caller);
        case Period.Afternoon:
          return String.Format("Buenas tardes {0}!", caller);
        case Period.Night:
          return String.Format("Buenas noches {0}!", caller);
      }
      return "Error: Período no especificado.";
    }

    private string PortugueseGreetings(string caller) {
      switch (_period) {
        case Period.Morning:
          return String.Format("Bom dia {0}!", caller);
        case Period.Afternoon:
          return String.Format("Boa tarde {0}!", caller);
        case Period.Night:
          return String.Format("Boa noite {0}!", caller);
      }
      return "Erro: Período não especificado.";
    }

    #endregion
  }
}