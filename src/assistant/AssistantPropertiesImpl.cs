using System;
using omg.org.CORBA;
using tecgraf.openbus.exceptions;

namespace tecgraf.openbus.assistant {
  /// <summary>
  /// Representa um conjunto de parâmetros opcionais que podem ser utilizados 
  /// para definir parâmetros de configuração do Assistente.
  /// </summary>
  public abstract class AssistantPropertiesImpl : AssistantProperties {
    private float _interval = 1;
    private readonly OrbServices _orb = OrbServices.GetSingleton();

    /// <inheritdoc/>
    internal int IntervalMillis { set; get; }

      /// <inheritdoc/>
    public float Interval {
      get { return _interval; }
      set {
        if (value < 0.001) {
          throw new InvalidPropertyValueException("interval",
                                                  "O intervalo deve ser maior que 0,001s.");
        }
        _interval = value;
        IntervalMillis = (int) Math.Ceiling(value*1000);
      }
    }

    /// <inheritdoc/>
    public OrbServices ORB {
      get { return _orb; }
    }

    /// <inheritdoc/>
    public ConnectionProperties ConnectionProperties { get; set; }

    /// <inheritdoc/>
    public OnLoginFailure LoginFailureCallback { get; set; }

    /// <inheritdoc/>
    public OnRegisterFailure RegisterFailureCallback { get; set; }

    /// <inheritdoc/>
    public OnRemoveOfferFailure RemoveOfferFailureCallback { get; set; }

    /// <inheritdoc/>
    public OnFindFailure FindFailureCallback { get; set; }

    /// <inheritdoc/>
    public OnStartSharedAuthFailure StartSharedAuthFailureCallback { get; set; }

    /// <inheritdoc/>
    public LoginType Type { get; internal set; }
  }
}