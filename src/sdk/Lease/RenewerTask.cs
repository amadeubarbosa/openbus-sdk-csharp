using System;
using System.Text;
using System.Threading;
using log4net;
using omg.org.CORBA;
using tecgraf.openbus.core.v2_00.services.access_control;

namespace tecgraf.openbus.sdk.lease
{
  /// <summary>
  /// Tarefa respons�vel por renovar o <i>lease</i> perante o servi�o de 
  /// controle de acesso.
  /// </summary>
  internal class RenewerTask
  {
    #region Fields

    private static readonly ILog Logger = LogManager.GetLogger(typeof(RenewerTask));

    /// <summary>
    /// A conex�o que deve ser mantida ativa.
    /// </summary>
    private readonly Connection _conn;

    /// <summary>
    /// Faceta de controle de acesso do barramento.
    /// </summary>
    private readonly AccessControl _ac;

    /// <summary>
    /// Indica se a <i>thread</i> deve continuar executando.
    /// </summary>
    private bool _mustContinue;

    private int _lease;

    #endregion

    #region Constructors

    /// <summary>
    /// Inicializa uma inst�ncia de renova��o de <i>lease</i>.
    /// </summary>
    /// <param name="connection">A credencial.</param>
    /// <param name="accessControlFacet">A faceta do barramento que permite a 
    /// renova��o de <i>lease</i>.</param>
    public RenewerTask(Connection connection, AccessControl accessControlFacet) {
      _conn = connection;
      _ac = accessControlFacet;
      _mustContinue = true;
    }

    #endregion

    #region Members

    /// <summary>
    /// Inicia a renova��o de lease.
    /// </summary>
    public void Run() {
      try {
        while (_mustContinue) {
          try {

            try {
              _lease = _ac.renew();
            }
            catch (NO_PERMISSION) {
              Logger.Debug("Imposs�vel renovar a credencial pois a conex�o n�o est� logada no barramento.");
            }

            if (_lease == 0) {
              _mustContinue = false;
              Logger.Warn("Falha na renova��o da credencial.");
              if (_conn.OnInvalidLoginCallback != null)
                Logger.Debug(_conn.OnInvalidLoginCallback.InvalidLogin(_conn)
                               ? "Credencial renovada ap�s callback de login inv�lido."
                               : "Credencial N�O renovada ap�s callback de login inv�lido.");
            }
            else {
              StringBuilder msg = new StringBuilder();
              msg.Append(DateTime.Now);
              msg.Append(" - Lease renovado. Pr�xima renova��o em ");
              msg.Append(_lease + " segundos.");
              Logger.Debug(msg.ToString());
            }
          }
          catch (AbstractCORBASystemException e) {
            Logger.Error("Erro ao tentar renovar o lease", e);
          }

          if (_mustContinue) {
            Thread.Sleep(_lease * 1000);
          }
        }
      }
      catch (ThreadInterruptedException) {
        Logger.Debug("Lease Interrompido");
      }
    }

    /// <summary>
    /// Solicita o fim da renova��o do <i>lease</i>.
    /// </summary>
    public void Finish() {
      _mustContinue = false;
    }

    public int Lease {
      get { return _lease; }
    }

    #endregion
  }
}
