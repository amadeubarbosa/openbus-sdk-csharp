using System.Security.Cryptography;
using tecgraf.openbus.core.v2_00.services.access_control;
using scs.core;
using tecgraf.openbus.core.v2_00.services.offer_registry;

namespace tecgraf.openbus.sdk
{
  /// <summary>
  /// API de acesso a um barramento OpenBus.
  /// </summary>
  public interface Openbus
  {
    /// <summary>
    /// Cria uma nova conexão com este barramento.
    /// </summary>
    Connection Connect();

    OfferRegistry OfferRegistry { get; }

    string BusId { get; }

    RSACryptoServiceProvider BusKey { get; }

    long Port { get; }

    string Host { get; }
  }
}
