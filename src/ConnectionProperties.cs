using tecgraf.openbus.exceptions;

namespace tecgraf.openbus {
  /// <summary>
  /// Interface utilizada para configurar as propriedades de uma conexão a ser criada.
  /// </summary>
  public interface ConnectionProperties {
    /// <summary>
    /// Define o valor da propriedade 'legacy.disable'.
    /// 
    /// Desabilita o suporte a chamadas usando protocolo OpenBus 1.5.
    /// Por padrão o suporte está habilitado.
    /// </summary>
    bool LegacyDisable { get; set; }

    /// <summary>
    /// Define o valor da propriedade 'legacy.delegate'.
    /// 
    /// Indica como é preenchido o campo 'delegate' das
    /// credenciais enviadas em chamadas usando protocolo OpenBus 1.5. Há
    /// duas formas possíveis (o padrão é 'caller'):
    ///   - caller: o campo 'delegate' é preenchido sempre com a entidade
    ///     do campo 'caller' da cadeia de chamadas.
    ///   - originator: o campo 'delegate' é preenchido sempre com a
    ///     entidade que originou a cadeia de chamadas, que é o primeiro
    ///     login do campo 'originators' ou o campo 'caller' quando este
    ///     é vazio.
    /// </summary>
    /// <exception cref="InvalidPropertyValueException">O valor fornecido não é um dos valores esperados.</exception>
    string LegacyDelegate { get; set; }

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
