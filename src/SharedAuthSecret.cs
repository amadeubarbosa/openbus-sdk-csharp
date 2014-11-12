namespace tecgraf.openbus {
  /// <summary>
  /// Segredo para compartilhamento de autenticação.
  /// 
  /// Objeto que representa uma tentativa de compartilhamento de autenticação
  /// através do compartilhamento de um segredo, que pode ser utilizado para
  /// realizar uma autenticação junto ao barramento em nome da mesma entidade que
  /// gerou e compartilhou o segredo.
  ///
  /// Cada segredo de autenticação compartilhada pertence a um único barramento e
  /// só pode utilizado em uma única autenticação.
  /// </summary>
  public interface SharedAuthSecret {
    /// <summary>
    /// Fornece o identificador do barramento em que o segredo pode ser utilizado.
    /// </summary>
    /// <returns>O identificador.</returns>
    string BusId { get; }

    /// <summary>
    /// Cancela o segredo caso esse ainda esteja ativo, de forma que ele não poderá
    /// ser mais utilizado.
    /// </summary>
    void Cancel();
  }
}
