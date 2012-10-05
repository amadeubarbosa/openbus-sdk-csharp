using System.Collections.Generic;
using System.Linq;
using omg.org.CORBA;
using tecgraf.openbus.core.v2_0.services.offer_registry;

namespace tecgraf.openbus.assistant {
  /// <summary>
  /// Classe estática com métodos utilitários para facilitar o uso do assistente.
  /// </summary>
  public static class Utils {
    /// <summary>
    /// Constrói sequencia de propriedades para determinada faceta e entidade.
    /// </summary>
    /// <param name="entity">Nome da entidade criadora da oferta.</param>
    /// <param name="facet">Nome da faceta que a oferta deve possuir.</param>
    /// <returns></returns>
    public static ServiceProperty[] CreateFacetAndEntityProperties(
      string entity,
      string facet) {
      return new[] {
                     new ServiceProperty("openbus.offer.entity", entity),
                     new ServiceProperty("openbus.component.facet", facet)
                   };
    }

    /// <summary>
    /// Obtém uma propriedade específica de um conjunto de propriedades de oferta.
    /// </summary>
    /// <param name="properties">Conjunto de propriedades de oferta.</param>
    /// <param name="name">Nome da propriedade a ser encontrada.</param>
    /// <returns>Propriedade encontrada ou null.</returns>
    public static string GetProperty(IEnumerable<ServiceProperty> properties,
                                     string name) {
      return (from property in properties
              where property.name.Equals(name)
              select property.value).FirstOrDefault();
    }

    /// <summary>
    /// Filtra ofertas e retorna somente as que se encontram responsivas.
    /// </summary>
    /// <param name="offers">Ofertas a ser verificadas por responsividade.</param>
    /// <returns>Conjunto de ofertas responsivas no momento do teste.</returns>
    public static ServiceOfferDesc[] FilterWorkingOffers(
      IEnumerable<ServiceOfferDesc> offers) {
      OrbServices orb = OrbServices.GetSingleton();
      return
        offers.Where(offer => !orb.non_existent(offer.service_ref)).ToArray();
    }
  }
}