using System;
using omg.org.CORBA;
using tecgraf.openbus.exceptions;

namespace tecgraf.openbus.assistant {
  /// <summary>
  /// Representa um conjunto de parâmetros opcionais que podem ser utilizados 
  /// para definir parâmetros de configuração do Assistente.
  /// </summary>
  public abstract class AssistantPropertiesImpl : AssistantProperties {
    private float _interval = 5;
    private readonly OrbServices _orb = OrbServices.GetSingleton();

    /// <inheritdoc/>
    internal int IntervalMillis {
      get {
        return (int)Math.Ceiling(Interval * 1000);
      }
    }

      /// <inheritdoc/>
    public float Interval {
      get { return _interval; }
      set {
        if (value < 1) {
          throw new InvalidPropertyValueException("interval",
                                                  "O intervalo de espera especificado é muito pequeno.");
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