/**
* \file RegistryService.h
*
* \brief API do servi�o de registro.
*
*/

#ifndef REGISTRYSERVICE_H_
#define REGISTRYSERVICE_H_

#include "../stubs/scs.hh"
#include "../stubs/registry_service.hh"

/**
* \brief Stubs dos servi�os b�sicos.
*/
namespace openbusidl {

/**
* \brief Stub do servi�o de registro.
*/
  namespace rs {

  /**
  * \class PropertyValue
  * \brief Representa um valor de uma propriedade. 
  *
  * Este valor � uma sequ�ncia de strings, no caso, o valor pode ser �nico, 
  * sendo representado portanto por somente uma string.
  *
  */

  /**
  * \struct Property
  * \brief Representa uma propriedade que � um par chave/valor.
  *
  * A chave deve ser uma string. O valor pode ser �nico ou m�ltiplo, sendo
  * representado atrav�s de uma sequ�ncia de strings. \see PropertyValue
  *
  */

  /**
  * \class PropertyList
  * \brief Lista de propriedades.
  */

  /**
  * \class ServiceOffer
  * \brief Oferta de servi�o
  *
  * Descreve um servi�o atrav�s de uma lista de propriedades e um componente 
  * SCS. \see PropertyList
  */

  /**
  * \class ServiceOfferList
  * \brief Lista de ofertas de servi�o.
  */

  }
}

/**
* \brief openbus
*/
namespace openbus {
/**
* \brief Servi�os b�sicos do Openbus.
*/
  namespace services {

    typedef openbusidl::rs::PropertyList PropertyList;
    typedef openbusidl::rs::ServiceOffer ServiceOffer;
    typedef openbusidl::rs::ServiceOfferList ServiceOfferList;
    typedef openbusidl::rs::ServiceOfferList_var ServiceOfferList_var;

  /**
  * \brief Auxilia na constru��o de uma lista de propriedades.
  */
    class PropertyListHelper {
      private:
        openbusidl::rs::PropertyList_var propertyList;
        int numElements;
      public:
        PropertyListHelper();
        ~PropertyListHelper();
        void add(
          const char* key,
          const char* value);
        openbusidl::rs::PropertyList_var getPropertyList();
    };

  /**
  * \brief Representa o servi�o de registro. 
  */
    class RegistryService {
      private:
      /**
      * Ponteiro para o servi�o de registro. 
      */
        openbusidl::rs::IRegistryService* rgs;
      public:
      
      /**
      * Cria um objeto que representa o servi�o de registro.
      *
      * @param[in] _rgs Stub do servi�o de registro. 
      */
        RegistryService(openbusidl::rs::IRegistryService* _rgs);

      /**
      * Busca servi�os.
      * @param[in] criteria Lista de propriedades que descrevem o servi�o 
      *   desejado.
      * @return A lista de ofertas de servi�o que atendem aos crit�rios 
      *   especificados no par�metro criteria.
      */
        ServiceOfferList* find(PropertyList criteria);

      /**
      * Registra um servi�o
      *
      * @param[in] serviceOffer Oferta do servi�o a ser registrado no 
      *    barramento.
      * @param[out] registryId Identifica��o do servi�o registrado.
      * @return Se o servi�o foi registrado com sucesso, o valor true � 
      *   retornado, caso contr�rio false � retornado.
      */
        bool Register(
          ServiceOffer serviceOffer,
          char*& registryId);

      /**
      * Remove uma oferta de servi�o.
      *
      * @param[in] registryId Identificador do registro da oferta de servi�o.
      * @return Se o servi�o foi removido com sucesso do barramento,
      *    o valor true � retornado, caso contr�rio false � retornado.
      */
        bool unregister(char* registryId);
    };
  }
}

#endif
