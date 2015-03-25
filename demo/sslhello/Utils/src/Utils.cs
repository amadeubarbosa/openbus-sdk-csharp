using System;
using System.Collections;
using Ch.Elca.Iiop;
using Ch.Elca.Iiop.Security.Ssl;
using tecgraf.openbus;

namespace Utils {
  public static class SSLUtils {
    public static void InitORBWithSSL(string clientUser, string clientThumbprint, string serverUser, string serverThumbprint, string serverSSLPort) {
      IDictionary props = new Hashtable();
      props[SslTransportFactory.CLIENT_AUTHENTICATION] =
        "Ch.Elca.Iiop.Security.Ssl.ClientMutualAuthenticationSpecificFromStore,SSLPlugin";
      // take certificates from the windows certificate store of the current user
      props[ClientMutualAuthenticationSpecificFromStore.STORE_LOCATION] =
        clientUser;
      props[ClientMutualAuthenticationSpecificFromStore.CLIENT_CERTIFICATE] =
        clientThumbprint;

      props[IiopChannel.CHANNEL_NAME_KEY] = "securedServerIiopChannel";
      props[IiopChannel.TRANSPORT_FACTORY_KEY] =
          "Ch.Elca.Iiop.Security.Ssl.SslTransportFactory,SSLPlugin";

      props[IiopServerChannel.PORT_KEY] = serverSSLPort;
      props[SslTransportFactory.SERVER_REQUIRED_OPTS] = "96";
      props[SslTransportFactory.SERVER_SUPPORTED_OPTS] = "96";
      props[SslTransportFactory.SERVER_AUTHENTICATION] =
          "Ch.Elca.Iiop.Security.Ssl.DefaultServerAuthenticationImpl,SSLPlugin";
      props[DefaultServerAuthenticationImpl.SERVER_CERTIFICATE] =
          serverThumbprint;
      props[DefaultServerAuthenticationImpl.STORE_LOCATION] = serverUser;

      ORBInitializer.InitORB(props);
    }
  }
}
