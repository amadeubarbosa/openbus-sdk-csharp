using System;
using System.IO;
using System.Reflection;
using System.Text;
using Ch.Elca.Iiop.Idl;
using demo.Properties;
using omg.org.CORBA;
using scs.core;
using tecgraf.openbus;
using tecgraf.openbus.core.v2_1.services;
using tecgraf.openbus.core.v2_1.services.access_control;
using tecgraf.openbus.core.v2_1.services.offer_registry;
using Utils;

namespace demo {
  /// <summary>
  /// Cliente da demo hello.
  /// </summary>
  internal static class HelloClient {
    private static void Main(String[] args) {
      // Obt�m dados atrav�s dos argumentos
      string busIOR = File.ReadAllText(args[0]);
      string domain = args[1];
      string entity = args[2];
      byte[] password = new ASCIIEncoding().GetBytes(args[3]);
      SSLUtils.InitORBWithSSL(args[4], args[5], args[6], args[7], args[8], args[9]);

      // Cria conex�o e a define como conex�o padr�o tanto para entrada como sa�da.
      // O uso exclusivo da conex�o padr�o (sem uso de current e callback de despacho) s� � recomendado para aplica��es que criem apenas uma conex�o e desejem utiliz�-la em todos os casos. Para situa��es diferentes, consulte o manual do SDK OpenBus e/ou outras demos.
      OpenBusContext context = ORBInitializer.Context;
      Connection conn = context.ConnectByReference((IComponent)OrbServices.CreateProxy(typeof(IComponent), busIOR));
      context.SetDefaultConnection(conn);

      string helloIDLType = Repository.GetRepositoryID(typeof (Hello));
      ServiceOfferDesc[] offers = null;
      try {
        // Faz o login
        conn.LoginByPassword(entity, password, domain);
        // Faz busca utilizando propriedades geradas automaticamente e propriedades definidas pelo servi�o espec�fico
        // propriedade gerada automaticamente
        ServiceProperty autoProp =
          new ServiceProperty("openbus.component.interface", helloIDLType);
        // propriedade definida pelo servi�o hello
        ServiceProperty prop = new ServiceProperty("offer.domain", "Demo Hello");
        ServiceProperty[] properties = {prop, autoProp};
        offers = context.OfferRegistry.findServices(properties);
      }
      catch (AccessDenied) {
        Console.WriteLine(Resources.ClientAccessDenied + entity + ".");
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
          // caso seja uma exce��o lan�ada pelo SDK, ser� uma NO_PERMISSION
          npe = e.InnerException as NO_PERMISSION;
        }
        if ((npe == null) && (!(e is NO_PERMISSION))) {
          // caso n�o seja uma NO_PERMISSION n�o � uma exce��o esperada ent�o deixamos passar.
          throw;
        }
        npe = npe ?? (NO_PERMISSION) e;
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
              // utiliza o servi�o
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
                // caso seja uma exce��o lan�ada pelo SDK, ser� uma NO_PERMISSION
                npe = e.InnerException as NO_PERMISSION;
              }
              if ((npe == null) && (!(e is NO_PERMISSION))) {
                // caso n�o seja uma NO_PERMISSION n�o � uma exce��o esperada ent�o deixamos passar.
                throw;
              }
              npe = npe ?? (NO_PERMISSION) e;
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