using System;
using System.Collections.Generic;
using System.Text;

namespace OpenbusAPI.Lease
{
  /// <summary>
  /// Define como informar que a renova��o do <i>lease</i> expirou.
  /// </summary>
  public interface LeaseExpiredCallback
  {
    /// <summary>
    /// Informa que o <i>lease</i> expirou e n�o ser� mais renovado pelo <seealso cref=""/>
    /// </summary>
    /// /// O <i>lease</i> expirou e n�o ser� mais renovado pelo {@link LeaseRenewer};
    void expired();
  }
}
