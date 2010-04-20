

namespace OpenbusAPI.Lease
{
  /// <summary>
  /// Define como informar que a renova��o do <i>lease</i> expirou.
  /// </summary>
  public interface LeaseExpiredCallback
  {
    /// <summary>
    /// Informa que o <i>lease</i> expirou e n�o ser� mais renovado pelo
    /// <seealso cref="LeaseRenewer"/>
    /// </summary>
    void Expired();
  }
}
