using System.Threading;

namespace tecgraf.openbus.lease {
  /// <summary>
  /// Classe respons�vel por renovar o <i>lease</i>.
  /// </summary>
  internal class LeaseRenewer {
    #region Fields

    /// <summary>
    /// O renovador de lease.
    /// </summary>
    private RenewerTask _renewer;

    /// <summary>
    /// A thread respons�vel por renovar o <i>lease</i>.
    /// </summary>
    private readonly Thread _leaseThread;

    #endregion

    #region Constructors

    /// <summary>
    /// Inicializa um renovador de <i>lease</i>.
    /// </summary>
    /// <param name="connection">A conex�o que deve ser renovada.</param>
    /// <param name="lease">O tempo de <i>lease</i>.</param>
    public LeaseRenewer(Connection connection, int lease) {
      _renewer = new RenewerTask(connection, lease);
      _leaseThread = new Thread(_renewer.Run)
                     {Name = "LeaseRenewer", IsBackground = true};
    }

    #endregion

    #region Members

    /// <summary>
    /// Inicia uma renova��o de <i>lease</i>.
    /// </summary>
    public void Start() {
      _leaseThread.Start();
    }

    /// <summary>
    /// Solicita o fim da renova��o do <i>lease</i>.
    /// </summary>
    public void Finish() {
      _renewer.Finish();
      // Decidimos remover o join pois a thread de renova��o pode executar c�digo do usu�rio, o que poderia levar a um deadlock.
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