using System;
using tecgraf.openbus;

namespace demo {
  /// <summary>
  /// Implementa��o do servant Greetings.
  /// </summary>  
  public class GreetingsImpl : MarshalByRefObject, Greetings {
    #region Fields

    private readonly Language _language;
    private readonly Period _period;

    internal enum Language {
      English,
      Spanish,
      Portuguese
    }

    internal enum Period {
      Morning,
      Afternoon,
      Night
    }

    #endregion

    #region Constructors

    internal GreetingsImpl(Language language, Period period) {
      _language = language;
      _period = period;
    }

    #endregion

    #region Greetings Members

    public string sayGreetings() {
      string caller = ORBInitializer.Context.CallerChain.Caller.entity;
      switch (_language) {
        case Language.English:
          return EnglishGreetings(caller);
        case Language.Spanish:
          return SpanishGreetings(caller);
        case Language.Portuguese:
          return PortugueseGreetings(caller);
      }
      return "Erro: l�ngua n�o especificada.";
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
          return String.Format("Buenos d�as {0}!", caller);
        case Period.Afternoon:
          return String.Format("Buenas tardes {0}!", caller);
        case Period.Night:
          return String.Format("Buenas noches {0}!", caller);
      }
      return "Error: Per�odo no especificado.";
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
      return "Erro: Per�odo n�o especificado.";
    }

    #endregion

    public override object InitializeLifetimeService() {
      return null;
    }
  }
}