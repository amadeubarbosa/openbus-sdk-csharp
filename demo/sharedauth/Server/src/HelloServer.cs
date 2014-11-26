using System;
using System.Reflection;
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
  /// Servidor da demo shared auth.
  /// </summary>
  internal static class HelloServer {
    private static Connection _conn;
    private static ServiceOffer _offer;

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
        new DefaultComponentContext(new ComponentId("hello", 1, 0, 0, ".net"));

      // Cria a faceta Hello para o componente
      component.AddFacet("Hello", Repository.GetRepositoryID(typeof (Hello)),
                         new HelloImpl());

      // Define propriedades para a oferta de serviço a ser registrada no barramento
      IComponent ic = component.GetIComponent();
      ServiceProperty[] properties = {
                                       new ServiceProperty("offer.domain",
                                                           "Demo SharedAuth")
                                     };

      // Cria conexão e a define como conexão padrão tanto para entrada como saída.
      // O uso exclusivo da conexão padrão (sem uso de current e callback de despacho) só é recomendado para aplicações que criem apenas uma conexão e desejem utilizá-la em todos os casos. Para situações diferentes, consulte o manual do SDK OpenBus e/ou outras demos.
      OpenBusContext context = ORBInitializer.Context;
      _conn = context.CreateConnection(host, port);
      context.SetDefaultConnection(_conn);

      bool failed = true;
      try {
        // Faz o login
        _conn.LoginByCertificate(entity, privateKey);
        // Registra a oferta no barramento
        _offer = context.OfferRegistry.registerService(ic, properties);
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
      finally {
        if (failed) {
          Exit(1);
        }
      }

      // Mantém a thread ativa para aguardar requisições
      Console.WriteLine(Resources.ServerOK);
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