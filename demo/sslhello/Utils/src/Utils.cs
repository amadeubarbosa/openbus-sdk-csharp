using System.Collections;
using Ch.Elca.Iiop;
using Ch.Elca.Iiop.Security.Ssl;
using tecgraf.openbus;

namespace Utils {
  public static class SSLUtils {
    public static void InitORBWithSSL(string clientUser, string clientThumbprint, string serverUser, string serverThumbprint, ushort serverSSLPort, ushort serverOpenPort, bool clientAuthenticationRequirement, bool serverAuthenticationRequirement, string encryption, bool checkCertificateRevocation, bool checkServerName) {
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
