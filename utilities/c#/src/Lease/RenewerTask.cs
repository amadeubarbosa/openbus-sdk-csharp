using System;
using System.Collections.Generic;
using System.Text;
using openbusidl.acs;
using OpenbusAPI.Lease;
using System.Threading;
using OpenbusAPI.Logger;

namespace OpenbusAPI.Lease
{
  /// <summary>
  /// Tarefa respons�vel por renovar o <i>lease</i> perante o servi�o de controle de acesso.
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


    #region Constructors

    /// <summary>
    /// Inicializa uma inst�ncia de renova��o de <i>lease</i>.
    /// </summary>
    /// <param name="credential">A credencial.</param>
    /// <param name="provider">A faceta do servi�o de controle de acesso provedora de <i>lease</i>.</param>
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
      bool ok = false;
      int newLease = -1;
      while (this.mustContinue) {
        try {
          ok = this.leaseProvider.renewLease(this.credential, out newLease);
          if (!ok) {
            Log.LEASE.Warn("Falha na renova��o da credencial.");
            this.expiredCallback.expired();
            return;
          }
        }
        catch (System.Exception e) {
          Log.LEASE.Error("Erro ao tentar renovar o lease", e);
        }

        StringBuilder msg = new StringBuilder();
        msg.Append(DateTime.Now);
        msg.Append(" - Lease renovado. Pr�xima renova��o em ");
        msg.Append(newLease + " segundos.");
        Log.LEASE.Debug(msg.ToString());

        Thread.Sleep(newLease * 1000);
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
