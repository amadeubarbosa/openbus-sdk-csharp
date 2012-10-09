using System;
using omg.org.CORBA;
using scs.core;
using tecgraf.openbus.core.v2_0.services.offer_registry;
using tecgraf.openbus.exceptions;

namespace tecgraf.openbus.assistant {
  /// <summary>
  /// Representa um conjunto de parâmetros opcionais que podem ser utilizados 
  /// para definir parâmetros de configuração do Assistente.
  /// </summary>
  public abstract class AssistantPropertiesImpl : AssistantProperties {
    private int _interval = 1;
    private readonly OrbServices _orb = OrbServices.GetSingleton();
    private OnLoginFailure _loginFailureCallback = OnLoginFailure;
    private OnRegisterFailure _registerFailureCallback = OnRegisterFailure;
    private OnRemoveOfferFailure _removeOfferFailureCallback = OnRemoveOfferFailure;
    private OnFindFailure _findFailureCallback = OnFindFailure;
    private OnStartSharedAuthFailure _startSharedAuthFailureCallback = OnStartSharedAuthFailure;

    /// <inheritdoc/>
    public int Interval {
      get { return _interval; }
      set {
        if (value < -1) {
          throw new InvalidPropertyValueException("interval",
                                                  "O intervalo deve ser positivo, 0 ou -1.");
        }
        _interval = value;
      }
    }

    /// <inheritdoc/>
    public OrbServices ORB {
      get { return _orb; }
    }

    /// <inheritdoc/>
    public ConnectionProperties ConnectionProperties { get; set; }

    /// <inheritdoc/>
    public OnLoginFailure LoginFailureCallback {
      get { return _loginFailureCallback; }
      set { _loginFailureCallback = value; }
    }

    /// <inheritdoc/>
    public OnRegisterFailure RegisterFailureCallback {
      get { return _registerFailureCallback; }
      set { _registerFailureCallback = value; }
    }

    /// <inheritdoc/>
    public OnRemoveOfferFailure RemoveOfferFailureCallback {
      get { return _removeOfferFailureCallback; }
      set { _removeOfferFailureCallback = value; }
    }

    /// <inheritdoc/>
    public OnFindFailure FindFailureCallback {
      get { return _findFailureCallback; }
      set { _findFailureCallback = value; }
    }

    public OnStartSharedAuthFailure StartSharedAuthFailureCallback {
      get { return _startSharedAuthFailureCallback; }
      set { _startSharedAuthFailureCallback = value; }
    }

    /// <inheritdoc/>
    public LoginType Type { get; internal set; }

    private static void OnLoginFailure(Assistant assistant, Exception e) {
      // não faz nada
    }

    private static void OnRegisterFailure(Assistant assistant,
                                          IComponent component,
                                          ServiceProperty[] props, Exception e) {
      // não faz nada
    }

    private static void OnRemoveOfferFailure(Assistant assistant,
                                             IComponent component,
                                             ServiceProperty[] props,
                                             Exception e) {
      // não faz nada
    }

    private static void OnFindFailure(Assistant assistant, Exception e) {
      // não faz nada
    }

    private static void OnStartSharedAuthFailure (Assistant assistant, Exception e) {
      // não faz nada
    }
  }
}