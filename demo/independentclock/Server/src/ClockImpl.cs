using System;

namespace demo {
  /// <summary>
  /// Implementação do servant Clock.
  /// </summary>  
  public class ClockImpl : MarshalByRefObject, Clock {
    #region Clock Members

    public long getTimeInTicks() {
      DateTime now = DateTime.Now;
      Console.WriteLine("Hora atual: {0:HH:mm:ss}", now);
      return now.Ticks;
    }

    #endregion

    public override object InitializeLifetimeService() {
      return null;
    }
  }
}