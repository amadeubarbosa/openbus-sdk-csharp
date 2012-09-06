using System;
using System.Text;
using Ch.Elca.Iiop.Idl;
using demo.Properties;
using omg.org.CORBA;
using tecgraf.openbus;
using tecgraf.openbus.core.v2_0.services;
using tecgraf.openbus.core.v2_0.services.access_control;
using tecgraf.openbus.core.v2_0.services.offer_registry;

namespace demo {
  /// <summary>
  /// Cliente da demo greetings.
  /// </summary>
  internal static class GreetingsClient {
    private static void Main(String[] args) {
      // Obt�m dados atrav�s dos argumentos
      string host = args[0];
      ushort port = Convert.ToUInt16(args[1]);
      string entity = args[2];
      byte[] password = new ASCIIEncoding().GetBytes(args[3] ?? entity);

      // Cria conex�o e a define como conex�o padr�o tanto para entrada como sa�da.
      // O uso exclusivo da conex�o padr�o (sem uso de current e callback de despacho) s� � recomendado para aplica��es que criem apenas uma conex�o e desejem utiliz�-la em todos os casos. Para situa��es diferentes, consulte o manual do SDK OpenBus e/ou outras demos.
      OpenBusContext context = ORBInitializer.Context;
      Connection conn = context.CreateConnection(host, port, null);
      context.SetDefaultConnection(conn);

      // Pergunta ao usu�rio qual l�ngua deseja utilizar
      Console.WriteLine(Resources.GreetingsWhichLanguage);
      string language = Console.ReadLine();
      if (language == null) {
        Console.WriteLine(Resources.GreetingsLanguageReadErrorMsg);
        Environment.Exit(1);
      }

      string greetingsIDLType = Repository.GetRepositoryID(typeof (Greetings));
      ServiceOfferDesc[] offers = null;
      try {
        // Faz o login
        conn.LoginByPassword(entity, password);
        // Faz busca utilizando propriedades geradas automaticamente e propriedades definidas pelo servi�o espec�fico
        // propriedade gerada automaticamente
        ServiceProperty autoProp1 =
          new ServiceProperty("openbus.component.interface", greetingsIDLType);
        // propriedade definida pelo servi�o greetings
        ServiceProperty prop = new ServiceProperty("offer.domain",
                                                   "Demo Greetings");
        ServiceProperty[] properties;
        if (!language.Equals("")) {
          ServiceProperty autoProp2 = new ServiceProperty(
            "openbus.component.name", language.ToLower());
          properties = new[] {prop, autoProp1, autoProp2};
        }
        else {
          Console.WriteLine(Resources.GreetingsNoLanguageSpecified);
          properties = new[] {prop, autoProp1};
        }

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
      catch (NO_PERMISSION e) {
        if (e.Minor == NoLoginCode.ConstVal) {
          Console.WriteLine(Resources.NoLoginCodeErrorMsg);
        }
        else {
          throw;
        }
      }

      // analiza as ofertas encontradas
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
              int hours = DateTime.Now.TimeOfDay.Hours;
              MarshalByRefObject greetObj = hours < 12
                                              ? serviceOfferDesc.service_ref.
                                                  getFacetByName("GoodMorning")
                                              : serviceOfferDesc.service_ref.
                                                  getFacetByName(hours >= 18
                                                                   ? "GoodNight"
                                                                   : "GoodAfternoon");
              if (greetObj == null) {
                Console.WriteLine(Resources.FacetNotFoundInOffer);
                continue;
              }
              Greetings greetings = greetObj as Greetings;
              if (greetings == null) {
                Console.WriteLine(Resources.FacetFoundWrongType);
                continue;
              }
              Console.WriteLine(Resources.OfferFound);
              // utiliza o servi�o
              Console.WriteLine((string) greetings.sayGreetings());
            }
            catch (TRANSIENT) {
              Console.WriteLine(Resources.ServiceTransientErrorMsg);
            }
            catch (COMM_FAILURE) {
              Console.WriteLine(Resources.ServiceCommFailureErrorMsg);
            }
            catch (NO_PERMISSION e) {
              bool found = false;
              string message = String.Empty;
              switch (e.Minor) {
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
      Console.WriteLine(Resources.ClientOK);
      Console.ReadLine();
    }
  }
}