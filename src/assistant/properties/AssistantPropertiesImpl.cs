using omg.org.CORBA;
using tecgraf.openbus.exceptions;

namespace tecgraf.openbus.assistant.properties {
  /// <summary>
  /// 
  /// </summary>
  public abstract class AssistantPropertiesImpl : AssistantProperties {
    private int _interval;
    private readonly ORB _orb = OrbServices.GetSingleton();

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

    public OnFailureCallback FailureCallback { get; set; }

    public LoginType Type { get; internal set; }
  }
}