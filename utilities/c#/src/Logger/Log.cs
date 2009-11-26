

namespace OpenbusAPI.Logger
{
  /// <summary>
  /// Classe respons�vel pelo Log do Openbus
  /// </summary>
  public class Log
  {
    #region Fields

    /// <summary>
    /// Log dos mecanismos comuns.
    /// </summary>
    public static readonly ILogger COMMON = new ApacheLog("Openbus-Common");
    /// <summary>
    /// Log utilizado pelos servi�os.
    /// </summary>
    public static readonly ILogger SERVICES = new ApacheLog("Openbus-Service");
    /// <summary>
    /// Log do mecanismo de lease.
    /// </summary>
    public static readonly ILogger LEASE = new ApacheLog("Openbus-Lease");
    /// <summary>
    /// Log do mecanismo de interceptadores.
    /// </summary>
    public static readonly ILogger INTERCEPTORS = new ApacheLog("Openbus-Interceptors");

    #endregion

    #region Members

    /// <summary>
    /// Define o n�vel de todos os logs do Openbus
    /// </summary>
    /// <param name="level">O n�vel de log.</param>
    public static void setLogsLevel(Level level) {
      COMMON.SetLevel(level);
      SERVICES.SetLevel(level);
      LEASE.SetLevel(level);
      INTERCEPTORS.SetLevel(level);
    }

    #endregion
  }
}
