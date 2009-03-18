/*
** openbus.h
*/

#ifndef OPENBUS_H_
#define OPENBUS_H_

#include "services/AccessControlService.h"
#include "services/RegistryService.h"

#include "openbus/common/ORBInitializerImpl.h"
#include <scs/core/ComponentBuilderOrbix.h>

#include <omg/orb.hh>
#include <it_ts/thread.h>
#include <it_ts/mutex.h>

using namespace openbusidl::acs;
IT_USING_NAMESPACE_STD

namespace openbus {
  class COMMUNICATION_FAILURE {};
  class LOGIN_FAILURE {};

  typedef openbusidl::acs::Credential_var Credential_var;

  class Openbus {
    private:

    /* Par�metro argc da linha de comando. */
      int _argc;

    /* Par�metro argv da linha de comando. */
      char** _argv;

    /* Inicializador do ORB. */
      static common::ORBInitializerImpl* ini;

    /* ORB */
      CORBA::ORB* orb;

    /* POA */
      PortableServer::POA* poa;

    /* F�brica de componentes SCS. */
      scs::core::ComponentBuilder* componentBuilder;

    /* Gerenciador do POA. */
      PortableServer::POAManager_var poa_manager;

    /* Servi�o de acesso. */
      services::AccessControlService* accessControlService;

    /* Servi�o de registro. */
      services::RegistryService* registryService;

    /* Intervalo de tempo que determina quando a credencial expira. */
      Lease lease;

    /* Credencial de identifica��o do usu�rio frente ao barramento. */
      Credential* credential;

    /* Host de localiza��o do barramento. */
      char* hostBus;

    /* Porta de localiza��o do barramento. */
      unsigned short portBus;

    /* Mutex */
      IT_Mutex* mutex;

    /* Poss�veis estados para a conex�o. */
      enum ConnectionStates {
        CONNECTED,
        DISCONNECTED
      };

    /* Indica o estado da conex�o. */
      ConnectionStates connectionState;

      unsigned long timeRenewing;

      void commandLineParse(
        int argc,
        char** argv);

    /* Inicializa um valor default para o host e porta do barramento. */
      void initializeHostPort();

    /* Cria implicitamente um ORB e um POA. */
      void createOrbPoa();

    /* Registra os interceptadores cliente e servidor. */
      void registerInterceptors();

    /* Cria um estado novo. */
      void newState();

      IT_Thread renewLeaseIT_Thread;

      class RenewLeaseThread : public IT_ThreadBody {
        private:
          Openbus* bus;
        public:
          RenewLeaseThread(Openbus* _bus);
          void* run();
      };

    public:

    /* Construtor
    *  Cria uma refer�ncia para um determinado barramento.
    *  A localiza��o do barramento pode ser fornecida atrav�s dos par�metros de linha comando
    *  -OpenbusHost e -OpenbusPort.
    */
      Openbus(
        int argc,
        char** argv);

    /* Construtor
    *  Cria uma refer�ncia para um determinado barramento.
    *  A localiza��o do barramento � fornecida atrav�s dos par�metros host e port.
    */
      Openbus(
        int argc,
        char** argv,
        char* host,
        unsigned short port);

      ~Openbus();

    /* Inicializa uma refer�ncia a um barramento.
    *  Um ORB e POA s�o criado implicitamente.
    *  Os par�metros argc e argv s�o repassados para a fun��o CORBA::ORB_init().
    *  A f�brica de componentes SCS � criada.
    *  Os argumentos Openbus de linha de comando (argc e argv) s�o tratados.
    */
      void init();

    /* Inicializa uma refer�ncia a um barramento.
    *  Um ORB e POA s�o passados explicitamente pelo usu�rio.
    *  A f�brica de componentes SCS � criada.
    *  Os argumentos Openbus de linha de comando (argc e argv) s�o tratados.
    */
      void init(
        CORBA::ORB_ptr _orb,
        PortableServer::POA* _poa);

    /* Retorna a f�brica de componentes. */
      scs::core::ComponentBuilder* getComponentBuilder();

    /* Retorna a credencial interceptada pelo interceptador servidor. */
      Credential_var getCredentialIntercepted();

    /* Retorna o servi�o de acesso. */
      services::AccessControlService* getAccessControlService();

    /* Retorna a credencial de identifica��o do usu�rio frente ao barramento. */
      Credential* getCredential();

    /* Retorna o intervalo de tempo que determina quando a credencial expira. */
      Lease getLease();

    /* Realiza uma tentativa de conex�o com o barramento.
    *  Par�metros de entrada:
    *    user: Nome do usu�rio.
    *    password: Senha do usu�rio.
    *  Se a tentativa for bem sucedida, uma inst�ncia que representa o servi�o de registro � retornada,
    *  caso contr�rio duas exce��es podem ser lan�adas:
    *    LOGIN_FAILURE: O par nome de usu�rio e senha n�o foram validados.
    *    COMMUNICATION_FAILURE: Alguma falha de comunica��o com o barramento ocorreu.
    */
      services::RegistryService* connect(
        const char* user,
        const char* password)
        throw (COMMUNICATION_FAILURE, LOGIN_FAILURE);

    /* Desfaz a conex�o atual.
    *  Uma requisi��o remota logout() � realizada.
    *  Antes da chamada logout() um estado de *desconectando* � assumido,
    *  impedindo assim que a renova��o de credencial seja realizada durante
    *  o processo.
    *  Se nenhuma conex�o estiver ativa, o valor de retorno � false.
    *  Retorno: Caso a conex�o seja desfeita, true � retornado, caso contr�rio,
    *  o valor de retorno � false.
    */
      bool disconnect();

    /* Loop que processa requisi��es CORBA. [execu��o do orb->run()]. */
      void run();
  };
}

#endif
