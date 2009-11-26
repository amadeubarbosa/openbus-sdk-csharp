using System;


namespace OpenbusAPI.Exception
{
  /// <summary>
  /// Indica uma exce��o de credencial inv�lida
  /// </summary>
  [Serializable]
  public class InvalidCredentialException : OpenbusException
  {
    /// <inheritdoc />
    public InvalidCredentialException() { }

    /// <inheritdoc />
    public InvalidCredentialException(string message) : base(message) { }

    /// <inheritdoc />
    public InvalidCredentialException(string message, System.Exception inner) 
      : base(message, inner) { }
  }
}
