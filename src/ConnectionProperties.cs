using tecgraf.openbus.exceptions;

namespace tecgraf.openbus {
  /// <summary>
  /// Interface utilizada para configurar as propriedades de uma conexão a ser criada.
  /// </summary>
  public interface ConnectionProperties {
    /// <summary>
    /// Define o valor da propriedade 'legacy.disable'.
    /// 
    /// Desabilita o suporte a chamadas usando protocolo OpenBus 2.0.
    /// Por padrão o suporte está habilitado.
    /// </summary>
    bool LegacyDisable { get; set; }

    /// <summary>
    /// Define o valor da propriedade 'access.key'.
    /// 
    /// Chave de acesso a ser utiliza internamente para a geração de
    /// credenciais que identificam as chamadas através do barramento. A chave
    /// deve ser uma chave privada RSA de 2048 bits (256 bytes). Quando essa
    /// propriedade não é fornecida, uma chave de acesso é gerada 
    /// automaticamente.
    /// </summary>
    /// <exception cref="InvalidPropertyValueException">A chave fornecida não contém dados da chave privada ou não foi gerada pelo SDK do OpenBus.</exception>
    PrivateKey AccessKey { get; set; }
  }
}
