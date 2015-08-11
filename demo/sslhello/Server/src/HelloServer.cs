using System;
using System.IO;
using System.Reflection;
using System.Threading;
using Ch.Elca.Iiop.Idl;
using Scs.Core;
using demo.Properties;
using omg.org.CORBA;
using scs.core;
using tecgraf.openbus;
using tecgraf.openbus.core.v2_1.services;
using tecgraf.openbus.core.v2_1.services.access_control;
using tecgraf.openbus.core.v2_1.services.offer_registry;
using tecgraf.openbus.security;
using Utils;

namespace demo {
  /// <summary>
  /// Servidor da demo hello.
  /// </summary>
  internal static class HelloServer {
    private static Connection _conn;
    private static ServiceOffer _offer;

    private static void Main(String[] args) {
      // Registra handler para o caso do processo ser finalizado
      AppDomain.CurrentDomain.ProcessExit += CurrentDomainProcessExit;

      // Obt�m dados atrav�s dos argumentos
      string busIOR = File.ReadAllText(args[0]);
      string entity = args[1];
      PrivateKey privateKey = Crypto.ReadKeyFile(args[2]);
      SSLUtils.InitORBWithSSL(args[3], args[4], args[5], args[6], args[7], args[8]);

      // Cria o componente que conter� as facetas do servidor
      ComponentContext component =
        new DefaultComponentContext(new ComponentId("hello", 1, 0, 0, ".net"));

      // Cria a faceta Hello para o componente
      component.AddFacet("Hello", Repository.GetRepositoryID(typeof (Hello)),
                         new HelloImpl());

      // Define propriedades para a oferta de servi�o a ser registrada no barramento
      IComponent ic = component.GetIComponent();
      ServiceProperty[] properties = {
                                       new ServiceProperty("offer.domain",
                                                           "Demo Hello")
                                     };

      // Cria conex�o e a define como conex�o padr�o tanto para entrada como sa�da.
      // O uso exclusivo da conex�o padr�o (sem uso de current e callback de despacho) s� � recomendado para aplica��es que criem apenas uma conex�o e desejem utiliz�-la em todos os casos. Para situa��es diferentes, consulte o manual do SDK OpenBus e/ou outras demos.
      OpenBusContext context = ORBInitializer.Context;
      _conn = context.ConnectByReference((IComponent)OrbServices.CreateProxy(typeof(IComponent), busIOR));
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
      finally {
        if (failed) {
          Exit(1);
        }
      }

      // Mant�m a thread ativa para aguardar requisi��es
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