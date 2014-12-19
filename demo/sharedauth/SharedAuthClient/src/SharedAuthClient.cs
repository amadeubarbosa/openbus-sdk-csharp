using System;
using System.IO;
using System.Reflection;
using Ch.Elca.Iiop.Idl;
using demo.Properties;
using omg.org.CORBA;
using tecgraf.openbus;
using tecgraf.openbus.core.v2_1.services;
using tecgraf.openbus.core.v2_1.services.access_control;
using tecgraf.openbus.core.v2_1.services.offer_registry;
using tecgraf.openbus.exceptions;

namespace demo {
  internal static class SharedAuthClient {
    private static void Main(String[] args) {
      // Obtém dados através dos argumentos
      string host = args[0];
      ushort port = Convert.ToUInt16(args[1]);
      string loginFile = args[2];

      ORBInitializer.InitORB();
      OpenBusContext context = ORBInitializer.Context;

      // Lê o arquivo com o segredo. Talvez seja interessante para a aplicação
      // trocar esses dados de outra forma (encriptados por exemplo).
      // Além disso, o cliente escreve apenas uma vez esses dados, que têm
      // validade igual ao lease do login dele, portanto uma outra forma mais
      // dinâmica seria mais eficaz. No entanto, isso foge ao escopo dessa demo.
      byte[] encoded = File.ReadAllBytes(loginFile);
      SharedAuthSecret secret = context.DecodeSharedAuth(encoded);

      // Cria conexão e a define como conexão padrão tanto para entrada como saída.
      // O uso exclusivo da conexão padrão (sem uso de current e callback de despacho) só é recomendado para aplicações que criem apenas uma conexão e desejem utilizá-la em todos os casos. Para situações diferentes, consulte o manual do SDK OpenBus e/ou outras demos.
      Connection conn = context.ConnectByAddress(host, port);
      context.SetDefaultConnection(conn);

      string helloIDLType = Repository.GetRepositoryID(typeof (Hello));
      ServiceOfferDesc[] offers = null;
      try {
        // Faz o login por autenticação compartilhada
        conn.LoginBySharedAuth(secret);
        // Faz busca utilizando propriedades geradas automaticamente e propriedades definidas pelo serviço específico
        // propriedade gerada automaticamente
        ServiceProperty autoProp =
          new ServiceProperty("openbus.component.interface", helloIDLType);
        // propriedade definida pelo serviço hello
        ServiceProperty prop = new ServiceProperty("offer.domain",
                                                   "Demo SharedAuth");
        ServiceProperty[] properties = { prop, autoProp };
        offers = context.OfferRegistry.findServices(properties);
      }
      catch (AccessDenied) {
        Console.WriteLine(Resources.ClientAccessDenied +
                          Resources.SharedAuthEntity);
      }
      catch (InvalidLoginProcessException) {
        Console.WriteLine(Resources.SharedAuthInvalidLoginProcessErrorMsg);
      }
      catch (ServiceFailure e) {
        Console.WriteLine(Resources.BusServiceFailureErrorMsg);
        Console.WriteLine(e);
      }
      catch (TRANSIENT) {
        Console.WriteLine(Resources.BusTransientErrorMsg);
      }
      catch (COMM_FAILURE) {
        Console.WriteLine(Resources.BusCommFailureErrorMsg);
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
        if (npe.Minor == NoLoginCode.ConstVal) {
          Console.WriteLine(Resources.NoLoginCodeErrorMsg);
        }
        else {
          throw;
        }
      }

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

      try {
        conn.Logout();
      }
      catch (ServiceFailure e) {
        Console.WriteLine(Resources.BusServiceFailureErrorMsg);
        Console.WriteLine(e);
      }
      catch (TRANSIENT) {
        Console.WriteLine(Resources.BusTransientErrorMsg);
      }
      catch (COMM_FAILURE) {
        Console.WriteLine(Resources.BusCommFailureErrorMsg);
      }
      if (!failed) {
        Console.WriteLine(Resources.ClientOK);
      }
      Console.ReadKey();
    }
  }
}