using System;
using System.IO;
using System.Reflection;
using System.Text;
using Ch.Elca.Iiop.Idl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using omg.org.CORBA;
using tecgraf.openbus.assistant;
using tecgraf.openbus.core.v2_1;
using tecgraf.openbus.core.v2_1.credential;
using tecgraf.openbus.core.v2_1.services.access_control;
using tecgraf.openbus.core.v2_1.services.offer_registry;
using tecgraf.openbus.interop.protocol.Properties;

namespace tecgraf.openbus.interop.protocol {
  /// <summary>
  /// Cliente do teste de interoperabilidade protocol.
  /// </summary>
  [TestClass]
  internal static class ProxyClient {
    private static void Main() {
      string hostName = DemoConfig.Default.busHostName;
      ushort hostPort = DemoConfig.Default.busHostPort;

      FileInfo logFileInfo = new FileInfo(DemoConfig.Default.openbusLogFile);
      XmlConfigurator.ConfigureAndWatch(logFileInfo);

      ConsoleAppender appender = new ConsoleAppender {
        Threshold = Level.Fatal,
        Layout =
          new SimpleLayout(),
      };
      BasicConfigurator.Configure(appender);

      // credential reset tests
      CredentialResetTest[] resetCases = new CredentialResetTest[1];
      CredentialReset tempReset = new CredentialReset { session = 2^32 - 1, challenge = CreateSecret(EncryptedBlockSize.ConstVal) };
      resetCases[0] = new CredentialResetTest { Reset = tempReset, Expected = InvalidRemoteCode.ConstVal };

      // no permission tests
      NoPermissionTest[] noPermissionCases = new NoPermissionTest[12];
      noPermissionCases[0] = new NoPermissionTest { Raised = 0, Expected = 0 };
      noPermissionCases[1] = new NoPermissionTest { Raised = InvalidCredentialCode.ConstVal, Expected = InvalidRemoteCode.ConstVal };
      noPermissionCases[2] = new NoPermissionTest { Raised = InvalidChainCode.ConstVal, Expected = InvalidChainCode.ConstVal };
      noPermissionCases[3] = new NoPermissionTest { Raised = UnverifiedLoginCode.ConstVal, Expected = UnverifiedLoginCode.ConstVal };
      noPermissionCases[4] = new NoPermissionTest { Raised = UnknownBusCode.ConstVal, Expected = UnknownBusCode.ConstVal };
      noPermissionCases[5] = new NoPermissionTest { Raised = InvalidPublicKeyCode.ConstVal, Expected = InvalidPublicKeyCode.ConstVal };
      noPermissionCases[6] = new NoPermissionTest { Raised = NoCredentialCode.ConstVal, Expected = NoCredentialCode.ConstVal };
      noPermissionCases[7] = new NoPermissionTest { Raised = NoLoginCode.ConstVal, Expected = InvalidRemoteCode.ConstVal };
      noPermissionCases[8] = new NoPermissionTest { Raised = InvalidRemoteCode.ConstVal, Expected = InvalidRemoteCode.ConstVal };
      noPermissionCases[9] = new NoPermissionTest { Raised = UnavailableBusCode.ConstVal, Expected = InvalidRemoteCode.ConstVal };
      noPermissionCases[10] = new NoPermissionTest { Raised = InvalidTargetCode.ConstVal, Expected = InvalidRemoteCode.ConstVal };
      noPermissionCases[11] = new NoPermissionTest { Raised = InvalidLoginCode.ConstVal, Expected = InvalidRemoteCode.ConstVal };

      ConnectionProperties props = new ConnectionPropertiesImpl();
      OpenBusContext context = ORBInitializer.Context;
      Connection conn = context.CreateConnection(hostName, hostPort, props);
      context.SetDefaultConnection(conn);

      const string userLogin = "interop_protocol_csharp_client";
      byte[] userPassword = new ASCIIEncoding().GetBytes(userLogin);

      conn.LoginByPassword(userLogin, userPassword);

      // propriedades geradas automaticamente
      ServiceProperty prop1 = new ServiceProperty("openbus.component.interface", Repository.GetRepositoryID(typeof(Server)));
      // propriedade definida pelo servidor protocol
      ServiceProperty prop2 = new ServiceProperty("offer.domain",
                                                  "Interoperability Tests");

      ServiceProperty[] properties = { prop1, prop2 };
      ServiceOfferDesc[] offers = context.OfferRegistry.findServices(properties);

      if (offers.Length < 1) {
        Console.WriteLine("O serviço Server não se encontra no barramento.");
        Environment.Exit(1);
      }
      if (offers.Length > 1) {
        Console.WriteLine("Existe mais de um serviço Server no barramento.");
      }

      bool foundOne = false;
      foreach (ServiceOfferDesc serviceOfferDesc in offers) {
        try {
          string found = Utils.GetProperty(serviceOfferDesc.properties, "openbus.offer.entity");
          Console.WriteLine("Entidade encontrada: " + found);
          MarshalByRefObject serverProxyObj =
            serviceOfferDesc.service_ref.getFacet(
              Repository.GetRepositoryID(typeof(Server)));
          if (serverProxyObj == null) {
            Console.WriteLine(
              "Não foi possível encontrar uma faceta com esse nome.");
            continue;
          }
          Server serverProxy = serverProxyObj as Server;
          if (serverProxy == null) {
            Console.WriteLine("Faceta encontrada não implementa Server.");
            continue;
          }
          foundOne = true;
          // inicio dos testes
          serverProxy.NonBusCall();
          foreach (CredentialResetTest test in resetCases) {
            bool error = false;
            try {
              serverProxy.ResetCredentialWithChallenge(test.Reset.session,
                test.Reset.challenge);
            }
            catch (Exception e) {
              NO_PERMISSION npe = null;
              if (e is TargetInvocationException) {
                npe = e.InnerException as NO_PERMISSION;
              }
              if ((npe == null) && (!(e is NO_PERMISSION))) {
                throw;
              }
              npe = npe ?? e as NO_PERMISSION;
              error = true;
              Assert.AreEqual(test.Expected, npe.Minor);
              Assert.AreEqual(CompletionStatus.Completed_No, npe.Status);
            }
            Assert.IsTrue(error);
          }

          foreach (NoPermissionTest test in noPermissionCases) {
            bool error = false;
            try {
              serverProxy.RaiseNoPermission(test.Raised);
            }
            catch (Exception e) {
              NO_PERMISSION npe = null;
              if (e is TargetInvocationException) {
                npe = e.InnerException as NO_PERMISSION;
              }
              if ((npe == null) && (!(e is NO_PERMISSION))) {
                throw;
              }
              npe = npe ?? e as NO_PERMISSION;
              error = true;
              Assert.AreEqual(test.Expected, npe.Minor);
              Assert.AreEqual(CompletionStatus.Completed_No, npe.Status);
            }
            Assert.IsTrue(error);
          }
        }
        catch (TRANSIENT) {
          Console.WriteLine(
            "Uma das ofertas obtidas é de um cliente inativo. Tentando a próxima.");
        }
      }
      conn.Logout();
      Assert.IsTrue(foundOne);
      Console.WriteLine("Fim.");
    }

    private static byte[] CreateSecret(int size) {
      byte[] array = new byte[size];
      for (int i = 0; i < size; i++) {
        array[i] = 121;
      }
      return array;
    }
  }

  class CredentialResetTest{
    public CredentialReset Reset;
    public int Expected;
  }

  class NoPermissionTest{
    public int Raised;
    public int Expected;
  }
}
