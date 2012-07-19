using System.Threading;
using tecgraf.openbus.core.v2_0.services.access_control;

namespace tecgraf.openbus.lease {
  /// <summary>
  /// Classe responsável por renovar o <i>lease</i>.
  /// </summary>
  internal class LeaseRenewer {
    #region Fields

    /// <summary>
    /// O renovador de lease.
    /// </summary>
    private RenewerTask _renewer;

    /// <summary>
    /// A thread responsável por renovar o <i>lease</i>.
    /// </summary>
    private readonly Thread _leaseThread;

    #endregion

    #region Constructors

    /// <summary>
    /// Inicializa um renovador de <i>lease</i>.
    /// </summary>
    /// <param name="connection">A conexão que deve ser renovada.</param>
    /// <param name="lease">O tempo de <i>lease</i>.</param>
    public LeaseRenewer(Connection connection, int lease) {
      _renewer = new RenewerTask(connection, lease);
      _leaseThread = new Thread(_renewer.Run)
                     {Name = "LeaseRenewer", IsBackground = true};
    }

    #endregion

    #region Members

    /// <summary>
    /// Inicia uma renovação de <i>lease</i>.
    /// </summary>
    public void Start() {
      _leaseThread.Start();
    }

    /// <summary>
    /// Solicita o fim da renovação do <i>lease</i>.
    /// </summary>
    public void Finish() {
      _renewer.Finish();
      // Não devo dar join "em mim mesmo" para evitar deadlock
      if (!Thread.CurrentThread.ManagedThreadId.Equals(_leaseThread.ManagedThreadId)) {
        _leaseThread.Join();
      }
      _renewer = null;
    }

    public int Lease {
      get {
        if (_renewer != null) {
          return _renewer.Lease;
        }
        return -1;
      }
    }

    #endregion
  }
}