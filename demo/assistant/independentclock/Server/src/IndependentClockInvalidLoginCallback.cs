using System;
using System.Threading;
using demo.Properties;
using omg.org.CORBA;
using tecgraf.openbus;
using tecgraf.openbus.core.v2_0.services;
using tecgraf.openbus.core.v2_0.services.access_control;
using tecgraf.openbus.exceptions;

namespace demo {
  internal class IndependentClockInvalidLoginCallback : InvalidLoginCallback {
    private readonly string _entity;
    private readonly PrivateKey _privKey;
    private readonly Registerer _registerer;
    private readonly int _waitTime;

    internal IndependentClockInvalidLoginCallback(string entity, PrivateKey privKey, Registerer registerer, int waitTime) {
      _entity = entity;
      _privKey = privKey;
      _registerer = registerer;
      _waitTime = waitTime;
    }

    public void InvalidLogin(Connection conn, LoginInfo login) {
      bool succeeded = false;
      while (!succeeded) {
        try {
          // Faz o login
          conn.LoginByCertificate(_entity, _privKey);
          succeeded = true;
        }
        // Login
        catch (AlreadyLoggedInException) {
          // Ignora o erro e retorna, pois já está reautenticado e portanto já há uma thread tentando registrar
          return;
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
        if (succeeded) {
          // Inicia o processo de re-registro da oferta
          _registerer.Activate();
        }
        else {
          Thread.Sleep(_waitTime * 1000);
        }
      }
    }
  }
}