using System;
using System.Threading;
using log4net;
using omg.org.CORBA;
using tecgraf.openbus.core.v2_0.services.access_control;

namespace tecgraf.openbus.lease {
  /// <summary>
  /// Tarefa responsável por renovar o <i>lease</i> perante o serviço de 
  /// controle de acesso.
  /// </summary>
  internal class RenewerTask {
    #region Fields

    private static readonly ILog Logger =
      LogManager.GetLogger(typeof (RenewerTask));

    private readonly AutoResetEvent _autoEvent = new AutoResetEvent(false);

    /// <summary>
    /// A conexão que deve ser mantida ativa.
    /// </summary>
    private readonly WeakReference _conn;

    #endregion

    #region Constructors

    /// <summary>
    /// Inicializa uma instância de renovação de <i>lease</i>.
    /// </summary>
    /// <param name="connection">A conexão que deve ser renovada.</param>
    /// <param name="lease">O tempo de <i>lease</i>.</param>
    public RenewerTask(Connection connection, int lease) {
      Lease = lease;
      ConnectionImpl conn = connection as ConnectionImpl;
      _conn = new WeakReference(conn);
    }

    #endregion

    #region Members

    /// <summary>
    /// Inicia a renovação de lease.
    /// </summary>
    public void Run() {
      try {
        while (!_autoEvent.WaitOne(Lease * 1000)) {
          try {
            ConnectionImpl conn = _conn.Target as ConnectionImpl;
            if (conn == null) {
              break;
            }
            conn.Manager.Requester = conn;
            AccessControl ac = conn.Acs;
            Lease = ac.renew();
            Logger.Debug(
              String.Format(
                "{0} - Lease renovado. Próxima renovação em {1} segundos.",
                DateTime.Now, Lease));
          }
          catch (NO_PERMISSION) {
            Logger.Warn(
              "Impossível renovar a credencial pois a conexão não está logada no barramento.");
          }
          catch (AbstractCORBASystemException e) {
            Logger.Error("Erro ao tentar renovar o lease", e);
          }
        }
        Logger.Debug("Thread de renovação de login finalizada.");
      }
      catch (ThreadInterruptedException) {
        Logger.Warn("Lease Interrompido");
      }
    }

    /// <summary>
    /// Solicita o fim da renovação do <i>lease</i>.
    /// </summary>
    public void Finish() {
      _autoEvent.Set();
    }

    public int Lease { get; private set; }

    #endregion
  }
}