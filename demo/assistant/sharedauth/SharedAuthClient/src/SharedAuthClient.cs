﻿using System;
using System.IO;
using System.Reflection;
using Ch.Elca.Iiop.Idl;
using demo.Properties;
using omg.org.CORBA;
using tecgraf.openbus;
using tecgraf.openbus.assistant;
using tecgraf.openbus.core.v2_0.services.access_control;
using tecgraf.openbus.core.v2_0.services.offer_registry;

namespace demo {
  internal static class SharedAuthClient {
    private static string _loginFile;

    private static void Main(String[] args) {
      // Obtém dados através dos argumentos
      string host = args[0];
      ushort port = Convert.ToUInt16(args[1]);
      _loginFile = args[2];

      // Usa o assistente do OpenBus para se conectar ao barramento e realizar a autenticação.
      AssistantProperties props = new SharedAuthProperties(SharedAuthObtainer) {
        LoginFailureCallback = LoginFailureCallback,
        FindFailureCallback = FindFailureCallback
      };
      Assistant assistant = new AssistantImpl(host, port, props);

      // Faz busca utilizando propriedades geradas automaticamente e propriedades definidas pelo serviço específico
      string helloIDLType = Repository.GetRepositoryID(typeof(Hello));
      // propriedade gerada automaticamente
      ServiceProperty autoProp =
        new ServiceProperty("openbus.component.interface", helloIDLType);
      // propriedade definida pelo serviço hello
      ServiceProperty prop = new ServiceProperty("offer.domain",
                                                 "Demo SharedAuth");
      ServiceOfferDesc[] offers =
        Utils.FilterWorkingOffers(assistant.FindServices(
          new[] { prop, autoProp }, -1));

      // analiza as ofertas encontradas
      bool failed = true;
      if (offers != null) {
        if (offers.Length < 1) {
          Console.WriteLine(Resources.ServiceNotFound);
        }
        else {
          if (offers.Length > 1) {
            Console.WriteLine(Resources.ServiceFoundMoreThanExpected);
          }
          foreach (ServiceOfferDesc serviceOfferDesc in offers) {
            Console.WriteLine(Resources.ServiceFoundTesting);
            try {
              MarshalByRefObject helloObj =
                serviceOfferDesc.service_ref.getFacet(helloIDLType);
              if (helloObj == null) {
                Console.WriteLine(Resources.FacetNotFoundInOffer);
                continue;
              }
              Hello hello = helloObj as Hello;
              if (hello == null) {
                Console.WriteLine(Resources.FacetFoundWrongType);
                continue;
              }
              Console.WriteLine(Resources.OfferFound);
              // utiliza o serviço
              hello.sayHello();
              failed = false;
              break;
            }
            catch (TRANSIENT) {
              Console.WriteLine(Resources.ServiceTransientErrorMsg);
            }
            catch (COMM_FAILURE) {
              Console.WriteLine(Resources.ServiceCommFailureErrorMsg);
            }
            catch (Exception e) {
              NO_PERMISSION npe = null;
              if (e is TargetInvocationException) {
                // caso seja uma exceção lançada pelo SDK, será uma NO_PERMISSION
                npe = e.InnerException as NO_PERMISSION;
              }
              if ((npe == null) && (!(e is NO_PERMISSION))) {
                // caso não seja uma NO_PERMISSION não é uma exceção esperada então deixamos passar.
                throw;
              }
              npe = npe ?? e as NO_PERMISSION;
              bool found = false;
              string message = String.Empty;
              switch (npe.Minor) {
                case NoLoginCode.ConstVal:
                  message = Resources.NoLoginCodeErrorMsg;
                  found = true;
                  break;
                case UnknownBusCode.ConstVal:
                  message = Resources.UnknownBusCodeErrorMsg;
                  found = true;
                  break;
                case UnverifiedLoginCode.ConstVal:
                  message = Resources.UnverifiedLoginCodeErrorMsg;
                  found = true;
                  break;
                case InvalidRemoteCode.ConstVal:
                  message = Resources.InvalidRemoteCodeErrorMsg;
                  found = true;
                  break;
              }
              if (found) {
                Console.WriteLine(message);
              }
              else {
                throw;
              }
            }
          }
          if (failed) {
            Console.WriteLine(Resources.OfferFunctionalNotFound);
          }
        }
      }

      assistant.Shutdown();
      Console.WriteLine(Resources.ClientOK);
      Console.ReadKey();
    }

    private static SharedAuthSecret SharedAuthObtainer() {
      // Lê o arquivo com o segredo. Talvez seja interessante para a aplicação
      // trocar esses dados de outra forma (encriptados por exemplo).
      // Além disso, o cliente escreve apenas uma vez esses dados, que têm
      // validade igual ao lease do login dele, portanto uma outra forma mais
      // dinâmica seria mais eficaz. No entanto, isso foge ao escopo dessa demo.
      OpenBusContext context = ORBInitializer.Context;
      byte[] encoded = File.ReadAllBytes(_loginFile);
      return context.DecodeSharedAuth(encoded);
    }

    private static void FindFailureCallback(Assistant assistant, Exception e) {
      Console.WriteLine(Resources.FindFailureCallback + e);
    }

    private static void LoginFailureCallback(Assistant assistant, Exception e) {
      Console.WriteLine(Resources.LoginFailureCallback + e);
    }
  }
}