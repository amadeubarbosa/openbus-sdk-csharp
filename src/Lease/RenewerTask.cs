using System;
using System.Text;
using System.Threading;
using omg.org.CORBA;
using tecgraf.openbus.core.v1_05.access_control_service;
using Tecgraf.Openbus.Logger;


namespace Tecgraf.Openbus.Lease
{
  /// <summary>
  /// Tarefa respons�vel por renovar o <i>lease</i> perante o servi�o de 
  /// controle de acesso.
  /// </summary>
  class RenewerTask
  {
    #region Fields

    /// <summary>
    /// A credencial.
    /// </summary>
    private Credential credential;

    /// <summary>
    /// A faceta do servi�o de controle de acesso provedora de <i>lease</i>.
    /// </summary>
    private ILeaseProvider leaseProvider;

    /// <summary>
    /// <i>Callback</i> usada para informar que a renova��o de um <i>lease</i>
    /// falhou.
    /// </summary>
    public LeaseExpiredCallback ExpiredCallback {
      set { expiredCallback = value; }
    }
    private LeaseExpiredCallback expiredCallback;

    /// <summary>
    /// Indica se a <i>thread</i> deve continuar executando.
    /// </summary>
    private bool mustContinue;

    #endregion

    #region Constants

    private const int DEFAULT_LEASE_TIME = 60;

    #endregion

    #region Constructors

    /// <summary>
    /// Inicializa uma inst�ncia de renova��o de <i>lease</i>.
    /// </summary>
    /// <param name="credential">A credencial.</param>
    /// <param name="provider">A faceta do servi�o de controle de acesso 
    /// provedora de <i>lease</i>.</param>
    public RenewerTask(Credential credential, ILeaseProvider provider) {
      this.credential = credential;
      this.leaseProvider = provider;
      this.expiredCallback = null;
      this.mustContinue = true;
    }

    #endregion

    #region Members

    /// <summary>
    /// Inicia a renova��o de lease.
    /// </summary>
    public void Run() {
      int newLease = DEFAULT_LEASE_TIME;
      try {
        while (this.mustContinue) {
          try {
            bool ok = false;

            try {
              ok = this.leaseProvider.renewLease(this.credential, out newLease);
            }
            catch (NO_PERMISSION) {
              ok = false;
            }

            if (!ok) {
              this.mustContinue = false;
              Log.LEASE.Warn("Falha na renova��o da credencial.");
              if (this.expiredCallback != null)
                this.expiredCallback.Expired();
            }
            else {
              StringBuilder msg = new StringBuilder();
              msg.Append(DateTime.Now);
              msg.Append(" - Lease renovado. Pr�xima renova��o em ");
              msg.Append(newLease + " segundos.");
              Log.LEASE.Debug(msg.ToString());
            }
          }
          catch (AbstractCORBASystemException e) {
            Log.LEASE.Error("Erro ao tentar renovar o lease", e);
          }

          if (this.mustContinue) {
            Thread.Sleep(newLease * 1000);
          }
        }
      }
      catch (ThreadInterruptedException) {
        Log.LEASE.Debug("Lease Interrompido");
      }
    }

    /// <summary>
    /// Solicita o fim da renova��o do <i>lease</i>.
    /// </summary>
    public void Finish() {
      this.mustContinue = false;
    }

    #endregion
  }
}
