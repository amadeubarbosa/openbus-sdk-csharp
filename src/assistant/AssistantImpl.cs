using System;
using omg.org.CORBA;
using scs.core;
using tecgraf.openbus.assistant.properties;
using tecgraf.openbus.core.v2_0.services.offer_registry;

namespace tecgraf.openbus.assistant {
  enum LoginType {
    Password,
    PrivateKey,
    SharedAuth
  }

  /// <inheritdoc/>
  public class AssistantImpl : Assistant {
    private readonly string _host;
    private readonly uint _port;
    private readonly AssistantProperties _properties;
    private readonly LoginType _loginType;
    private readonly PasswordProperties _passwordProperties;
    private readonly PrivateKeyProperties _privateKeyProperties;
    private readonly SharedAuthProperties _sharedAuthProperties;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="host"></param>
    /// <param name="port"></param>
    /// <param name="properties"></param>
    public AssistantImpl(string host, uint port, AssistantProperties properties) {
      Orb = OrbServices.GetSingleton();
      _host = host;
      _port = port;
      _properties = properties;
      _passwordProperties = properties as PasswordProperties;
      if (_passwordProperties != null) {
        _loginType = LoginType.Password;
      }
      else {
        _privateKeyProperties = properties as PrivateKeyProperties;
        if (_privateKeyProperties != null) {
          _loginType = LoginType.PrivateKey;
        }
        else {
          _sharedAuthProperties = properties as SharedAuthProperties;
          if (_sharedAuthProperties != null) {
            _loginType = LoginType.SharedAuth;
          }
          else {
            throw new ArgumentException("O conjunto properties deve ser do tipo PasswordProperties, PrivateKeyProperties ou SharedAuthProperties.");
          }
        }
      }
      // lança a thread que faz o login, registra, etc
      throw new NotImplementedException();
    }

    public void AddOffer(IComponent component, ServiceProperty[] properties) {
      throw new NotImplementedException();
    }

    public ServiceOfferDesc[] FindOffers(ServiceProperty[] properties, int retries) {
      throw new NotImplementedException();
    }

    public void Shutdown() {
      throw new NotImplementedException();
    }

    public ORB Orb { get; private set; }
  }
}
