/*
** RegistryService.h
**
** Header do m�dulo wrapper do servi�o de registro.
**
*/

#ifndef REGISTRYSERVICE_H_
#define REGISTRYSERVICE_H_

#include "../stubs/scs.hh"
#include "../stubs/registry_service.hh"

namespace openbus {
  namespace services {
//     typedef char** PropertyList;
    typedef openbusidl::rs::PropertyList PropertyList;
    typedef openbusidl::rs::ServiceOffer ServiceOffer;
    typedef openbusidl::rs::ServiceOfferList ServiceOfferList;

    class RegistryService {
      private:
      /* Ponteiro para o servi�o de registro. */
        openbusidl::rs::IRegistryService* rgs;
      public:
        RegistryService(openbusidl::rs::IRegistryService* _rgs);

      /*
      * Busca servi�os.
      * criteria: Lista de propriedades que descrevem o servi�o desejado.
      * Retorna uma lista de ofertas de servi�o.
      */
        ServiceOfferList* find(PropertyList criteria);

      /*
      * Registra um servi�o
      * Par�metros de entrada:
      *   serviceOffer: Oferta do servi�o a ser registrado no barramento.
      * Par�metros de sa�da:
      *   registryId: Identifica��o do servi�o registrado.
      */
        void Register(ServiceOffer serviceOffer, char* registryId);

         void Register(PropertyList propertyList, scs::core::IComponent* iComponent, char* registryId);
    };
  }
}

#endif
