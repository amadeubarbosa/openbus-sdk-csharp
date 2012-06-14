using System;

namespace Server {
  /// <summary>
  /// Implementação do servant Clock.
  /// </summary>  
  public class ClockImpl : MarshalByRefObject, Clock {
    #region Clock Members

    public long getTimeInTicks() {
      DateTime now = DateTime.Now;
      Console.WriteLine(String.Format("Hora atual: {0}:{1}:{2}", now.Hour,
                                      now.Minute, now.Second));
      return now.Ticks;
    }

    #endregion
  }
}