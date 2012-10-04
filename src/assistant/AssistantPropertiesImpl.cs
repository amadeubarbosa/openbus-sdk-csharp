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
    private int _interval;
    private readonly ORB _orb = OrbServices.GetSingleton();
    private OnLoginFailure _loginFailureCallback = OnLoginFailure;
    private OnRegisterFailure _registerFailureCallback = OnRegisterFailure;

    private OnRemoveOfferFailure _removeOfferFailureCallback =
      OnRemoveOfferFailure;

    private OnFindFailure _findFailureCallback = OnFindFailure;

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

    public ORB ORB {
      get { return _orb; }
    }

    public ConnectionProperties ConnectionProperties { get; set; }

    public OnLoginFailure LoginFailureCallback {
      get { return _loginFailureCallback; }
      set { _loginFailureCallback = value; }
    }

    public OnRegisterFailure RegisterFailureCallback {
      get { return _registerFailureCallback; }
      set { _registerFailureCallback = value; }
    }

    public OnRemoveOfferFailure RemoveOfferFailureCallback {
      get { return _removeOfferFailureCallback; }
      set { _removeOfferFailureCallback = value; }
    }

    public OnFindFailure FindFailureCallback {
      get { return _findFailureCallback; }
      set { _findFailureCallback = value; }
    }

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
  }
}