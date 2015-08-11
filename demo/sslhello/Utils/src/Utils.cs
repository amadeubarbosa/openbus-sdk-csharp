using System.Collections;
using Ch.Elca.Iiop;
using Ch.Elca.Iiop.Security.Ssl;
using tecgraf.openbus;

namespace Utils {
  public static class SSLUtils {
    public static void InitORBWithSSL(string clientUser, string clientThumbprint, string serverUser, string serverThumbprint, string serverSSLPort, string serverOpenPort) {
      Hashtable props = new Hashtable();
      props[IiopChannel.CHANNEL_NAME_KEY] = "SecuredServerIiopChannel";
      props[IiopChannel.TRANSPORT_FACTORY_KEY] =
          "Ch.Elca.Iiop.Security.Ssl.SslTransportFactory,SSLPlugin";
      props[Authentication.CheckCertificateRevocation] = false;
      props[SSLClient.CheckServerName] = false;
      props[SSLClient.ClientEncryptionType] = Encryption.EncryptionType.Required;
      props[SSLClient.ClientAuthentication] = SSLClient.ClientAuthenticationType.Supported;
      props[SSLClient.ServerAuthentication] = SSLClient.ServerAuthenticationType.Required;
      props[SSLClient.ClientAuthenticationClass] = typeof(ClientAuthenticationSpecificFromStore);
      props[ClientAuthenticationSpecificFromStore.StoreLocation] =
        clientUser;
      props[ClientAuthenticationSpecificFromStore.ClientCertificate] =
        clientThumbprint;

      props[SSLServer.ServerEncryptionType] = Encryption.EncryptionType.Required;
      props[SSLServer.ClientAuthentication] = SSLServer.ClientAuthenticationType.Required;
      props[SSLServer.ServerAuthentication] = SSLServer.ServerAuthenticationType.Supported;
      props[SSLServer.ServerAuthenticationClass] = typeof(DefaultServerAuthenticationImpl);
      props[DefaultServerAuthenticationImpl.ServerCertificate] =
        serverThumbprint;
      props[DefaultServerAuthenticationImpl.StoreLocation] = serverUser;
      props[SSLServer.SecurePort] = serverSSLPort;
      props[IiopServerChannel.PORT_KEY] = serverOpenPort;

      ORBInitializer.InitORB(props);
    }
  }
}
