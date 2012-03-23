using System;
using System.Text;
using System.Threading;
using log4net;
using omg.org.CORBA;
using tecgraf.openbus.core.v2_00.services.access_control;

namespace tecgraf.openbus.sdk.lease
{
  /// <summary>
  /// Tarefa responsável por renovar o <i>lease</i> perante o serviço de 
  /// controle de acesso.
  /// </summary>
  internal class RenewerTask
  {
    #region Fields

    private static readonly ILog Logger = LogManager.GetLogger(typeof(RenewerTask));

    /// <summary>
    /// A conexão que deve ser mantida ativa.
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

    private bool _justStarted;

    #endregion

    #region Constructors

    /// <summary>
    /// Inicializa uma instância de renovação de <i>lease</i>.
    /// </summary>
    /// <param name="connection">A credencial.</param>
    /// <param name="accessControlFacet">A faceta do barramento que permite a 
    /// renovação de <i>lease</i>.</param>
    /// <param name="lease">O tempo de <i>lease</i>.</param>
    public RenewerTask(Connection connection, AccessControl accessControlFacet, int lease) {
      Lease = lease;
      _conn = connection;
      _ac = accessControlFacet;
      _mustContinue = true;
      _justStarted = true;
    }

    #endregion

    #region Members

    /// <summary>
    /// Inicia a renovação de lease.
    /// </summary>
    public void Run() {
      try {
        while (_mustContinue) {
          if (!_justStarted) {
            try {
              try {
                Lease = _ac.renew();
              }
              catch (NO_PERMISSION) {
                Logger.Debug("Impossível renovar a credencial pois a conexão não está logada no barramento.");
              }

              if (Lease == 0) {
                _mustContinue = false;
                Logger.Warn("Falha na renovação da credencial.");
                if (_conn.OnInvalidLoginCallback != null)
                  Logger.Debug(_conn.OnInvalidLoginCallback.InvalidLogin(_conn)
                                 ? "Credencial renovada após callback de login inválido."
                                 : "Credencial NÃO renovada após callback de login inválido.");
              }
              else {
                StringBuilder msg = new StringBuilder();
                msg.Append(DateTime.Now);
                msg.Append(" - Lease renovado. Próxima renovação em ");
                msg.Append(Lease + " segundos.");
                Logger.Debug(msg.ToString());
              }
            }
            catch (AbstractCORBASystemException e) {
              Logger.Error("Erro ao tentar renovar o lease", e);
            }

          }
          if (_mustContinue) {
            Thread.Sleep(Lease * 1000);
            _justStarted = false;
          }
        }
      }
      catch (ThreadInterruptedException) {
        Logger.Debug("Lease Interrompido");
      }
    }

    /// <summary>
    /// Solicita o fim da renovação do <i>lease</i>.
    /// </summary>
    public void Finish() {
      _mustContinue = false;
    }

    public int Lease { get; private set; }

    #endregion
  }
}
