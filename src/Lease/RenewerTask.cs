using System;
using System.Threading;
using log4net;
using omg.org.CORBA;
using tecgraf.openbus.core.v2_0.services.access_control;
using tecgraf.openbus.exceptions;

namespace tecgraf.openbus.lease {
  /// <summary>
  /// Tarefa respons�vel por renovar o <i>lease</i> perante o servi�o de 
  /// controle de acesso.
  /// </summary>
  internal class RenewerTask {
    #region Fields

    private static readonly ILog Logger =
      LogManager.GetLogger(typeof (RenewerTask));

    private readonly AutoResetEvent _autoEvent = new AutoResetEvent(false);

    /// <summary>
    /// A conex�o que deve ser mantida ativa.
    /// </summary>
    private readonly ConnectionImpl _conn;

    /// <summary>
    /// Faceta de controle de acesso do barramento.
    /// </summary>
    private readonly AccessControl _ac;

    #endregion

    #region Constructors

    /// <summary>
    /// Inicializa uma inst�ncia de renova��o de <i>lease</i>.
    /// </summary>
    /// <param name="connection">A credencial.</param>
    /// <param name="accessControlFacet">A faceta do barramento que permite a 
    /// renova��o de <i>lease</i>.</param>
    /// <param name="lease">O tempo de <i>lease</i>.</param>
    public RenewerTask(Connection connection, AccessControl accessControlFacet,
                       int lease) {
      Lease = lease;
      _conn = connection as ConnectionImpl;
      if (_conn == null) {
        throw new OpenBusInternalException(
          "Imposs�vel criar renovador de credencial com conex�o nula ou inv�lida.");
      }
      _ac = accessControlFacet;
    }

    #endregion

    #region Members

    /// <summary>
    /// Inicia a renova��o de lease.
    /// </summary>
    public void Run() {
      try {
        _conn.Manager.Requester = _conn;
        while (!_autoEvent.WaitOne(Lease * 1000)) {
          try {
            Lease = _ac.renew();
            Logger.Debug(String.Format("{0} - Lease renovado. Pr�xima renova��o em {1} segundos.", DateTime.Now, Lease));
          }
          catch (NO_PERMISSION) {
            Logger.Warn(
              "Imposs�vel renovar a credencial pois a conex�o n�o est� logada no barramento.");
          }
          catch (AbstractCORBASystemException e) {
            Logger.Error("Erro ao tentar renovar o lease", e);
          }
        }
        Logger.Debug("Thread de renova��o de login finalizada.");
      }
      catch (ThreadInterruptedException) {
        Logger.Warn("Lease Interrompido");
      }
    }

    /// <summary>
    /// Solicita o fim da renova��o do <i>lease</i>.
    /// </summary>
    public void Finish() {
      _autoEvent.Set();
    }

    public int Lease { get; private set; }

    #endregion
  }
}