using System;
using demo.Properties;
using omg.org.CORBA;
using tecgraf.openbus;
using tecgraf.openbus.core.v2_0.services;
using tecgraf.openbus.core.v2_0.services.access_control;
using tecgraf.openbus.exceptions;

namespace demo {
  internal class DedicatedClockClientInvalidLoginCallback : InvalidLoginCallback {
    private readonly string _entity;
    private readonly byte[] _password;

    internal DedicatedClockClientInvalidLoginCallback(string entity,
                                                      byte[] password) {
      _entity = entity;
      _password = password;
    }

    public void InvalidLogin(Connection conn, LoginInfo login) {
      bool failed = true;
      do {
        try {
          // Faz o login
          conn.LoginByPassword(_entity, _password);
          failed = false;
        }
        // Login
        catch (AlreadyLoggedInException) {
          // Ignora o erro
          failed = false;
        }
        catch (AccessDenied) {
          Console.WriteLine(Resources.ServerAccessDenied);
        }
        catch (MissingCertificate) {
          Console.WriteLine(Resources.MissingCertificateForEntity + _entity);
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
      } while (failed && DedicatedClockClient.Retry());
    }
  }
}