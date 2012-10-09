using System;
using scs.core;
using tecgraf.openbus.core.v2_0.services.offer_registry;

namespace tecgraf.openbus.assistant {
  /// <summary>
  /// Callback de notificação de falhas capturadas pelo assistente durante o 
  /// processo de login.
  /// </summary>
  /// <param name="assistant">Assistente que chama a callback.</param>
  /// <param name="e">Objeto que descreve a falha ocorrida.</param>
  public delegate void OnLoginFailure(Assistant assistant, Exception e);

  /// <summary>
  /// Callback de notificação de falhas capturadas pelo assistente durante o 
  /// registro de ofertas de serviço.
  /// </summary>
  /// <param name="assistant">Assistente que chama a callback.</param>
  /// <param name="component">Componente sendo registrado.</param>
  /// <param name="props">Lista de propriedades com que o serviço deveria ter sido registrado.</param>
  /// <param name="e">Objeto que descreve a falha ocorrida.</param>
  public delegate void OnRegisterFailure(Assistant assistant,
                                         IComponent component,
                                         ServiceProperty[] props,
                                         Exception e);

  /// <summary>
  /// Callback de notificação de falhas capturadas pelo assistente durante a 
  /// remoção de ofertas de serviço.
  /// </summary>
  /// <param name="assistant">Assistente que chama a callback.</param>
  /// <param name="component">Componente sendo removido.</param>
  /// <param name="props">Lista de propriedades do serviço que deveria ter sido
  ///  removido.</param>
  /// <param name="e">Objeto que descreve a falha ocorrida.</param>
  public delegate void OnRemoveOfferFailure(Assistant assistant,
                                            IComponent component,
                                            ServiceProperty[] props,
                                            Exception e);

  /// <summary>
  /// Callback de notificação de falhas capturadas pelo assistente durante a 
  /// busca por ofertas de serviço.
  /// </summary>
  /// <param name="assistant">Assistente que chama a callback.</param>
  /// <param name="e">Objeto que descreve a falha ocorrida.</param>
  public delegate void OnFindFailure(Assistant assistant, Exception e);

  /// <summary>
  /// Callback de notificação de falhas capturadas pelo assistente durante a 
  /// inicialização de autenticação compartilhada.
  /// </summary>
  /// <param name="assistant">Assistente que chama a callback.</param>
  /// <param name="e">Objeto que descreve a falha ocorrida.</param>
  public delegate void OnStartSharedAuthFailure(Assistant assistant, Exception e);
}