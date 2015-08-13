using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Ch.Elca.Iiop;
using Ch.Elca.Iiop.Security.Ssl;
using omg.org.CORBA;
using tecgraf.openbus.core.v2_1.services.offer_registry;

namespace tecgraf.openbus.interop.utils {
  /// <summary>
  ///   Classe estática com métodos utilitários para facilitar o uso do assistente.
  /// </summary>
  public static class Utils {
    /// <summary>
    ///   Constrói sequencia de propriedades para determinada faceta e entidade.
    /// </summary>
    /// <param name="entity">Nome da entidade criadora da oferta.</param>
    /// <param name="facet">Nome da faceta que a oferta deve possuir.</param>
    /// <returns></returns>
    public static ServiceProperty[] CreateFacetAndEntityProperties(
      string entity,
      string facet) {
      return new[] {
        new ServiceProperty("openbus.offer.entity", entity),
        new ServiceProperty("openbus.component.facet", facet)
      };
    }

    /// <summary>
    ///   Obtém uma propriedade específica de um conjunto de propriedades de oferta.
    /// </summary>
    /// <param name="properties">Conjunto de propriedades de oferta.</param>
    /// <param name="name">Nome da propriedade a ser encontrada.</param>
    /// <returns>Propriedade encontrada ou null.</returns>
    public static string GetProperty(IEnumerable<ServiceProperty> properties,
      string name) {
      return (from property in properties
        where property.name.Equals(name)
        select property.value).FirstOrDefault();
    }

    /// <summary>
    ///   Filtra ofertas e retorna somente as que se encontram responsivas.
    ///   Qualquer erro encontrado ao tentar acessar uma oferta faz com que não
    ///   seja inclusa no conjunto retornado, inclusive erros NO_PERMISSION com
    ///   minor code NoLogin.
    /// </summary>
    /// <param name="offers">Ofertas a ser verificadas por responsividade.</param>
    /// <returns>Conjunto de ofertas responsivas no momento do teste.</returns>
    public static ServiceOfferDesc[] FilterWorkingOffers(
      IEnumerable<ServiceOfferDesc> offers) {
      OrbServices orb = OrbServices.GetSingleton();
      IList<ServiceOfferDesc> working = new List<ServiceOfferDesc>();
      foreach (ServiceOfferDesc offerDesc in offers) {
        try {
          if (!orb.non_existent(offerDesc.service_ref)) {
            working.Add(offerDesc);
          }
        }
        catch (Exception) {
          // não adiciona essa oferta
        }
      }
      return working.ToArray();
    }

    public static List<ServiceOfferDesc> FindOffer(OfferRegistry offers,
      ServiceProperty[] search, int count, int tries, int interval) {
      OrbServices orb = OrbServices.GetSingleton();
      List<ServiceOfferDesc> found = new List<ServiceOfferDesc>();
      for (int i = 0; i < tries; i++) {
        found.Clear();
        Thread.Sleep(interval * 1000);
        ServiceOfferDesc[] services = offers.findServices(search);
        if (services.Length > 0) {
          foreach (ServiceOfferDesc offerDesc in services) {
            try {
              if (!orb.non_existent(offerDesc.service_ref)) {
                found.Add(offerDesc);
              }
            }
            catch (Exception) {
              // não adiciona essa oferta
            }
          }
        }
        if (found.Count >= count) {
          return found;
        }
      }
      StringBuilder buffer = new StringBuilder();
      foreach (ServiceOfferDesc desc in found) {
        String name = GetProperty(desc.properties, "openbus.offer.entity");
        String login = GetProperty(desc.properties, "openbus.offer.login");
        buffer.AppendFormat("\n - {0} ({1})", name, login);
      }
      String msg =
        String
          .Format(
            "Não foi possível encontrar ofertas: found ({0}) expected({1}) tries ({2}) time ({3}){4}",
            found.Count, count, tries, tries * interval, buffer);
      throw new InvalidOperationException(msg);
    }

    public static void InitSSLORB(string clientUser, string clientThumbprint, string serverUser, string serverThumbprint, ushort serverSSLPort, ushort serverOpenPort, bool clientAuthenticationRequirement, bool serverAuthenticationRequirement, string encryption, bool checkCertificateRevocation, bool checkServerName) {
      Hashtable props = new Hashtable();
      props[IiopChannel.CHANNEL_NAME_KEY] = "SecuredIiopChannel";
      props[IiopChannel.TRANSPORT_FACTORY_KEY] =
          "Ch.Elca.Iiop.Security.Ssl.SslTransportFactory,SSLPlugin";
      if (clientThumbprint != null) {
        props[SSLClient.ClientAuthentication] = SSLClient.ClientAuthenticationType.Supported;
        props[ClientAuthenticationSpecificFromStore.ClientCertificate] =
          clientThumbprint;
        props[SSLClient.ClientAuthenticationClass] = typeof(ClientAuthenticationSpecificFromStore);
        props[ClientAuthenticationSpecificFromStore.StoreLocation] =
          clientUser;
      }
      else {
        props[SSLClient.ClientAuthentication] = SSLClient.ClientAuthenticationType.NotSupported;
        props[SSLClient.ClientAuthenticationClass] = typeof(DefaultClientAuthenticationImpl);
      }
      if (serverAuthenticationRequirement) {
        props[SSLClient.ServerAuthentication] = SSLClient.ServerAuthenticationType.Required;
      }

      props[SSLServer.ServerAuthenticationClass] = typeof(DefaultServerAuthenticationImpl);
      props[IiopServerChannel.PORT_KEY] = serverOpenPort;
      if (serverThumbprint != null) {
        props[SSLServer.ServerAuthentication] = SSLServer.ServerAuthenticationType.Supported;
        props[DefaultServerAuthenticationImpl.ServerCertificate] =
          serverThumbprint;
        props[DefaultServerAuthenticationImpl.StoreLocation] = serverUser;
        props[SSLServer.SecurePort] = serverSSLPort;
      }
      if (clientAuthenticationRequirement) {
        props[SSLServer.ClientAuthentication] = SSLServer.ClientAuthenticationType.Required;
      }

      props[SSLClient.ClientEncryptionType] = Encryption.EncryptionType.NotSupported;
      props[SSLServer.ServerEncryptionType] = Encryption.EncryptionType.NotSupported;
      switch (encryption.ToLower()) {
        case "required":
          props[SSLClient.ClientEncryptionType] = Encryption.EncryptionType.Required;
          props[SSLServer.ServerEncryptionType] = Encryption.EncryptionType.Required;
          break;
        case "supported":
          props[SSLClient.ClientEncryptionType] = Encryption.EncryptionType.Supported;
          props[SSLServer.ServerEncryptionType] = Encryption.EncryptionType.Supported;
          break;
      }

      props[Authentication.CheckCertificateRevocation] = checkCertificateRevocation;
      props[SSLClient.CheckServerName] = checkServerName;

      ORBInitializer.InitORB(props);
    }
  }
}