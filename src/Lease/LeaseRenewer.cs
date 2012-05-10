using System.Threading;
using tecgraf.openbus.core.v2_00.services.access_control;

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
    /// <param name="connection">A credencial.</param>
    /// <param name="accessControlFacet">A faceta do barramento que permite a 
    /// renovação de <i>lease</i>.</param>
    /// <param name="lease">O tempo de <i>lease</i>.</param>
    public LeaseRenewer(Connection connection, AccessControl accessControlFacet, int lease) {
      _renewer = new RenewerTask(connection, accessControlFacet, lease);
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
      _renewer = null;
      _leaseThread.Interrupt();
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