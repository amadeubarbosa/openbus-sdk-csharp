using System;
using omg.org.CORBA;
using scs.core;
using tecgraf.openbus.core.v2_0.services.access_control;
using tecgraf.openbus.core.v2_0.services.offer_registry;

namespace tecgraf.openbus.assistant {
  /// <summary>
  /// Assistente que auxilia a integração de uma aplicação a um barramento.
  /// 
  /// O assistente realiza tarefas de manutenção da integração da aplicação com o
  /// barramento. Tais tarefas incluem:
  /// - Restabelecimento de login, mesmo em virtude de falhas temporárias.
  /// - Registro de ofertas de serviço, mesmo após restabelecimento
  ///   de login.
  /// - Busca de ofetas de serviço disponíveis no barramento.
  /// 
  /// O assistente nunca lança exceções, pois todas as operações são realizadas
  /// de forma assíncrona. Eventuais falhas nessas operações assíncronas são 
  /// notificadas através de callbacks.
  /// </summary>
  public interface Assistant {
    /// <summary>
    /// Solicita que o assistente registre um serviço no barramento.
    /// 
    /// Esse método notifica o assistente de que o serviço fornecido deve ser
    /// mantido como uma oferta de serviço válida no barramento. Para tanto,
    /// sempre que o assistente restabelecer o login esse serviço será registrado
    /// novamente no barramento.
    ///
    /// Para que o registro de serviços seja bem sucedido é necessário que o ORB
    /// utilizado pelo assistente esteja processando chamadas, por exemplo,
    /// fazendo com que a aplicação mantenha ao menos uma thread ativa.
    ///
    /// Caso ocorram erros, a callback de tratamento de erro apropriada será
    /// chamada.
    /// </summary>
    /// <param name="component">Referência do serviço sendo ofertado.</param>
    /// <param name="properties">Propriedades do serviço sendo ofertado.</param>
    void RegisterService(IComponent component, ServiceProperty[] properties);

    /// <summary>
    /// Solicita que o assistente remova um serviço do barramento.
    /// 
    /// Esse método notifica o assistente de que o serviço não deve mais ser
    /// mantido como uma oferta de serviço válida no barramento. A oferta é
    /// removida de forma assíncrona e todas as tentativas de registro atuais
    /// são canceladas.
    /// 
    /// Caso ocorram erros, a callback de tratamento de erro apropriada será
    /// chamada.
    /// </summary>
    /// <param name="component">Referência do serviço a ser removido.</param>
    /// <returns>Conjunto de propriedades do serviço removido ou null caso o 
    /// serviço não esteja registrado no assistente.</returns>
    ServiceProperty[] UnregisterService(IComponent component);

    /// <summary>
    /// Busca por ofertas que apresentem um conjunto de propriedades definido.
    /// 
    /// Serão selecionadas apenas as ofertas de serviço que apresentem todas as 
    /// propriedades especificadas. As propriedades utilizadas nas buscas podem
    /// ser aquelas fornecidas no momento do registro da oferta de serviço, assim
    /// como as propriedades automaticamente geradas pelo barramento.
    /// 
    /// Caso ocorram erros, a callback de tratamento de erro apropriada será
    /// chamada. Se o número de tentativas se esgotar e não houver sucesso, uma
    /// sequência vazia será retornada.
    /// </summary>
    /// <param name="properties">Propriedades que as ofertas de serviços 
    /// encontradas devem apresentar.</param>
    /// <param name="retries">Parâmetro opcional indicando o número de novas 
    /// tentativas de busca de ofertas em caso de falhas, como o barramento 
    /// estar indisponível ou não ser possível estabelecer um login até o 
    /// momento. 'retries' com o valor 0 implica que a operação retorna 
    /// imediatamente após uma única tentativa. Para tentar indefinidamente o 
    /// valor de 'retries' deve ser -1. Entre cada tentativa é feita uma pausa 
    /// dada pelo parâmetro 'interval' fornecido na criação do assistente (veja
    /// a interface 'AssistantProperties').</param>
    /// <returns>Sequência de descrições de ofertas de serviço encontradas.</returns>
    ServiceOfferDesc[] FindServices(ServiceProperty[] properties, int retries = 0);

    /// <summary>
    /// Devolve uma lista de todas as ofertas de serviço registradas.
    ///
    /// Caso ocorram erros, a callback de tratamento de erro apropriada será
    /// chamada. Se o número de tentativas se esgotar e não houver sucesso, uma
    /// sequência vazia será retornada.
    /// </summary>
    /// <param name="retries">Parâmetro opcional indicando o número de novas 
    /// tentativas de busca de ofertas em caso de falhas, como o barramento 
    /// estar indisponível ou não ser possível estabelecer um login até o 
    /// momento. 'retries' com o valor 0 implica que a operação retorna 
    /// imediatamente após uma única tentativa. Para tentar indefinidamente o 
    /// valor de 'retries' deve ser -1. Entre cada tentativa é feita uma pausa 
    /// dada pelo parâmetro 'interval' fornecido na criação do assistente (veja
    /// a interface 'AssistantProperties').</param>
    /// <returns>Sequência de descrições de ofertas de serviço registradas.</returns>
    ServiceOfferDesc[] GetAllServices(int retries = 0);

    /// <summary>
    /// Inicia o processo de login por autenticação compartilhada.
    /// 
    /// A autenticação compartilhada permite criar um novo login compartilhando a
    /// mesma autenticação do login atual da conexão. As informações
    /// fornecidas por essa operação devem ser passadas para a operação
    /// 'loginBySharedAuth' para conclusão do processo de login por autenticação
    /// compartilhada. Isso deve ser feito dentro do tempo de lease definido pelo
    /// administrador do barramento. Caso contrário essas informações se tornam
    /// inválidas e não podem mais ser utilizadas para criar um login.
    ///
    /// Caso ocorram erros, a callback de tratamento de erro apropriada será
    /// chamada. Se o número de tentativas se esgotar e não houver sucesso, os
    /// dois valores retornados serão null.
    /// </summary>
    /// <param name="secret"> Segredo a ser fornecido na conclusão do processo de login.</param>
    /// <param name="retries">Parâmetro opcional indicando o número de novas 
    /// tentativas de busca de ofertas em caso de falhas, como o barramento 
    /// estar indisponível ou não ser possível estabelecer um login até o 
    /// momento. 'retries' com o valor 0 implica que a operação retorna 
    /// imediatamente após uma única tentativa. Para tentar indefinidamente o 
    /// valor de 'retries' deve ser -1. Entre cada tentativa é feita uma pausa 
    /// dada pelo parâmetro 'interval' fornecido na criação do assistente (veja
    /// a interface 'AssistantProperties').</param>
    /// <returns> Objeto que representa o processo de login iniciado.</returns>
    LoginProcess StartSharedAuth(out Byte[] secret, int retries = 0);

    /// <summary>
    /// Encerra o funcionamento do assistente liberando todos os recursos
    ///        alocados por ele.
    /// 
    /// Essa operação deve ser chamada antes do assistente ser descartado, pois
    /// como o assistente tem um funcionamento ativo, ele continua funcionando e
    /// consumindo recursos mesmo que a aplicação não tenha mais referências a ele.
    /// Em particular, alguns dos recursos gerenciados pelo assistente são:
    /// - Login no barramento;
    /// - Ofertas de serviço registradas no barramento;
    /// - Observadores de serviço registrados no barramento;
    /// - Threads de manutenção desses recursos no barramento;
    /// - Conexão default no ORB sendo utilizado;
    ///
    /// Em particular, o processamento de requisições do ORB (e.g. através da
    /// operação 'ORB::run()') não é gerido pelo assistente, portanto é
    /// responsabilidade da aplicação iniciar e parar esse processamento (e.g.
    /// através da operação 'ORB::shutdown()')
    /// </summary>
    void Shutdown();

    /// <summary>
    /// ORB utilizado pelo assistente.
    /// </summary>
    ORB Orb { get; }
  }
}
