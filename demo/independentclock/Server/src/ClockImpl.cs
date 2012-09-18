using System;

namespace demo {
  /// <summary>
  /// Implementa��o do servant Clock.
  /// </summary>  
  public class ClockImpl : MarshalByRefObject, Clock {
    #region Clock Members

    public long getTimeInTicks() {
      DateTime now = DateTime.Now;
      Console.WriteLine(
        String.Format("Hora atual: {0:HH:mm:ss}", now));
      return now.Ticks;
    }

    #endregion
  }
}