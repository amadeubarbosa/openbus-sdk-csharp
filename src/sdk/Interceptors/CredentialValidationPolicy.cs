
namespace tecgraf.openbus.sdk.Interceptors
{
  /// <summary>
  /// Define as políticas para a validação de credenciais interceptadas em um
  /// servidor.
  /// </summary>  
  public enum CredentialValidationPolicy
  {
    /// <summary>
    /// Indica que as credenciais interceptadas serão sempre validadas.   
    /// </summary>
    ALWAYS,
    /// <summary>
    /// Indica que as credenciais interceptadas serão validadas e armazenadas em um
    /// cache. 
    /// </summary>
    CACHED,
    /// <summary>
    /// Indica que as credenciais interceptadas não serão validadas.
    /// </summary>
    NONE
  }

}
