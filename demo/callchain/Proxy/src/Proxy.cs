using System;
using System.Threading;
using Ch.Elca.Iiop.Idl;
using Scs.Core;
using demo.Properties;
using omg.org.CORBA;
using scs.core;
using tecgraf.openbus;
using tecgraf.openbus.core.v2_0.services;
using tecgraf.openbus.core.v2_0.services.access_control;
using tecgraf.openbus.core.v2_0.services.offer_registry;
using tecgraf.openbus.security;

namespace demo {
  /// <summary>
  /// Proxy da demo CallChain.
  /// </summary>
  internal static class Proxy {
    private static Connection _conn;
    private static ServiceOffer _offer;
    internal static ServiceOfferDesc[] Offers;

    private static void Main(String[] args) {
      // Registra handler para o caso do processo ser finalizado
      AppDomain.CurrentDomain.ProcessExit += CurrentDomainProcessExit;

      // Obtém dados através dos argumentos
      string host = args[0];
      ushort port = Convert.ToUInt16(args[1]);
      string entity = args[2];
      PrivateKey privateKey = Crypto.ReadKeyFile(args[3]);

      // Cria o componente que conterá as facetas do servidor
      ComponentContext component =
        new DefaultComponentContext(new ComponentId("messengerproxy", 1, 0, 0,
                                                    ".net"));

      // Cria a faceta Messenger para o componente
      string messengerIDLType = Repository.GetRepositoryID(typeof (Messenger));
      component.AddFacet("Messenger", messengerIDLType, new ProxyMessengerImpl());

      // Define propriedades para a oferta de serviço a ser registrada no barramento
      IComponent ic = component.GetIComponent();
      ServiceProperty prop1 = new ServiceProperty("offer.domain",
                                                  "Demo CallChain");
      ServiceProperty prop2 = new ServiceProperty("offer.role",
                                                  "mensageiro proxy");
      ServiceProperty[] properties = {prop1, prop2};

      // Cria conexão e a define como conexão padrão tanto para entrada como saída.
      // O uso exclusivo da conexão padrão (sem uso de current e callback de despacho) só é recomendado para aplicações que criem apenas uma conexão e desejem utilizá-la em todos os casos. Para situações diferentes, consulte o manual do SDK OpenBus e/ou outras demos.
      OpenBusContext context = ORBInitializer.Context;
      _conn = context.CreateConnection(host, port);
      context.SetDefaultConnection(_conn);

      bool failed = true;
      try {
        // Faz o login
        _conn.LoginByCertificate(entity, privateKey);
        // Busca o Messenger real
        OfferRegistry offerRegistry = context.OfferRegistry;
        ServiceProperty autoProp =
          new ServiceProperty("openbus.component.interface", messengerIDLType);
        ServiceProperty findProp = new ServiceProperty("offer.role", "mensageiro real");
        ServiceProperty[] findProps = {autoProp, findProp, prop1};
        Offers = offerRegistry.findServices(findProps);
        // Registra a própria oferta no barramento
        _offer = offerRegistry.registerService(ic, properties);
        failed = false;
      }
        // Login
      catch (AccessDenied) {
        Console.WriteLine(Resources.ServerAccessDenied);
      }
      catch (MissingCertificate) {
        Console.WriteLine(Resources.MissingCertificateForEntity + entity);
      }
        // Registro
      catch (UnauthorizedFacets) {
        Console.WriteLine(Resources.UnauthorizedFacets);
      }
        // Barramento
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
      finally {
        if (failed) {
          Exit(1);
        }
      }

      // Mantém a thread ativa para aguardar requisições
      Console.WriteLine(Resources.CallChainProxyOK);
      Thread.Sleep(Timeout.Infinite);
    }

    private static void RemoveOfferAndLogout() {
      if (_offer != null) {
        try {
          Console.WriteLine(Resources.RemovingOffer);
          _offer.remove();
          Console.WriteLine(Resources.RemovedOffer);
        }
        catch (UnauthorizedOperation) {
          Console.WriteLine(Resources.UnauthorizedRemoveOffer);
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
      }
      if (_conn.Login.HasValue) {
        try {
          _conn.Logout();
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
      }
    }

    private static void CurrentDomainProcessExit(object sender, EventArgs e) {
      RemoveOfferAndLogout();
    }

    private static void Exit(int code) {
      RemoveOfferAndLogout();
      Console.WriteLine(Resources.PressAnyKeyToExit);
      Console.ReadKey();
      Environment.Exit(code);
    }
  }
}