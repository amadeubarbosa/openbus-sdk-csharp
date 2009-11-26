using System;


namespace OpenbusAPI.Logger
{
  /// <summary>
  /// Interface de Log do Openbus.
  /// </summary>
  public interface ILogger
  {
    /// <summary>
    /// Define o n�vel do log.
    /// </summary>
    /// <param name="level">O n�vel do log.</param>
    void SetLevel(Level level);

    /// <summary>
    /// Fornece o n�vel do log. 
    /// </summary>
    /// <returns>O n�vel do log.</returns>
    Level GetLevel();

    /// <summary>
    /// Registra uma mensagem fatal.
    /// </summary>
    /// <param name="message">A mensagem.</param>
    void Fatal(Object message);

    /// <summary>
    /// Registra uma mensagem fatal com uma exce��o associada.
    /// </summary>
    /// <param name="message">A mensagem.</param>
    /// <param name="exception">A exce��o</param>
    void Fatal(Object message, System.Exception exception);

    /// <summary>
    /// Registra uma mensagem de erro.
    /// </summary>
    /// <param name="message">A mensagem.</param>
    void Error(Object message);

    /// <summary>
    /// Registra uma mensagem de erro com uma exce��o associada.
    /// </summary>
    /// <param name="message">A mensagem.</param>
    /// <param name="exception">A exce��o.</param>
    void Error(Object message, System.Exception exception);

    /// <summary>
    /// Registra uma mensagem de warning. 
    /// </summary>
    /// <param name="message">A mensagem.</param>
    void Warn(Object message);

    /// <summary>
    /// Registra uma mensagem de warning com uma exce��o associada.
    /// </summary>
    /// <param name="message">A mensagem.</param>
    /// <param name="exception">A exce��o.</param>
    void Warn(Object message, System.Exception exception);

    /// <summary>
    /// Registra uma mensagem de informa��o.
    /// </summary>
    /// <param name="message">A mensagem.</param>
    void Info(Object message);

    /// <summary>
    /// Registra uma mensagem de informa��o com uma exce��o associada.
    /// </summary>
    /// <param name="message">A mensagem.</param>
    /// <param name="exception">A exce��o.</param>
    void Info(Object message, System.Exception exception);

    /// <summary>
    /// Registra uma mensagem de depura��o.
    /// </summary>
    /// <param name="message">A mensagem.</param>
    void Debug(Object message);

    /// <summary>
    /// Registra uma mensagem depura��o com uma exce��o associada.
    /// </summary>
    /// <param name="message">A mensagem.</param>
    /// <param name="exception">A exce��o.</param>
    void Debug(Object message, System.Exception exception);
  }

  /// <summary>
  /// Define os n�veis de log do Openbus.
  /// </summary>
  public enum Level
  {
    /// <summary>
    /// Niv�l fatal.
    /// </summary>
    FATAL,
    /// <summary>
    /// N�vel erro
    /// </summary>
    ERROR,
    /// <summary>
    /// N�vel warning
    /// </summary>
    WARN,
    /// <summary>
    /// N�vel informa��o
    /// </summary>
    INFO,
    /// <summary>
    /// N�vel depura��o.
    /// </summary>
    DEBUG
  }
}
