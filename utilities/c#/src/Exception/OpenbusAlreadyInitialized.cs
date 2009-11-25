using System;
using System.Collections.Generic;
using System.Text;
using OpenbusAPI.Exception;

namespace OpenbusAPI.Exception
{
  /// <summary>
  /// Indica uma exce��o de Openbus j� est� inicializado.
  /// </summary>
  class OpenbusAlreadyInitialized : OpenbusException
  {
    /// <inheritdoc />
    public OpenbusAlreadyInitialized() : base() { }


    /// <inheritdoc />
    public OpenbusAlreadyInitialized(string message) : base(message) { }


    /// <inheritdoc />
    public OpenbusAlreadyInitialized(string message, System.Exception inner) : base(message, inner) { }
  }
}
