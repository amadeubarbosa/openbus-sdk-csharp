using System;
using System.Collections.Generic;
using System.Threading;
using log4net;
using omg.org.CORBA;
using scs.core;
using tecgraf.openbus.assistant.properties;
using tecgraf.openbus.core.v2_0.services.access_control;
using tecgraf.openbus.exceptions;

namespace tecgraf.openbus.assistant.callbacks {
  internal class AssistantOnInvalidLoginCallback : InvalidLoginCallback {
    private static readonly ILog Logger =
      LogManager.GetLogger(typeof(AssistantOnInvalidLoginCallback));

    private readonly AssistantImpl _assistant;

    internal AssistantOnInvalidLoginCallback(AssistantImpl assistant) {
      _assistant = assistant;
    }

    public void InvalidLogin(Connection conn, LoginInfo login) {
      bool succeeded = false;
      while (!succeeded) {
        // remove todas as ofertas locais
        foreach (KeyValuePair<IComponent, Offeror> pair in _assistant.Offers) {
          pair.Value.Reset();
        }
        Exception caught = null;
        try {
          switch (_assistant.Properties.Type) {
            case LoginType.Password:
              PasswordProperties passwordProps =
                (PasswordProperties) _assistant.Properties;
              conn.LoginByPassword(passwordProps.Entity, passwordProps.Password);
              break;
            case LoginType.PrivateKey:
              PrivateKeyProperties privKeyProps =
                (PrivateKeyProperties) _assistant.Properties;
              conn.LoginByCertificate(privKeyProps.Entity,
                                      privKeyProps.PrivateKey);
              break;
            case LoginType.SharedAuth:
              SharedAuthProperties saProps =
                (SharedAuthProperties) _assistant.Properties;
              byte[] secret;
              LoginProcess lp = saProps.Callback.Invoke(out secret);
              conn.LoginBySharedAuth(lp, secret);
              break;
          }
          succeeded = true;
        }
        catch(AlreadyLoggedInException e) {
          caught = e;
          succeeded = true;
        }
        catch (NO_PERMISSION e) {
          if (e.Minor == NoLoginCode.ConstVal) {
            caught = e;
          }
          else {
            // provavelmente é um buug, então deixa estourar
            throw;
          }
        }
        catch (Exception e) {
          // passa qualquer erro para a aplicação, menos erros que pareçam bugs do SDK
          caught = e;
        }
        if (succeeded) {
          // Se houve exceção recebida e foi AlreadyLoggedIn, já existe uma thread tentando registrar as ofertas
          if ((caught as AlreadyLoggedInException) == null) {
            // Inicia o processo de re-registro das ofertas
            foreach (KeyValuePair<IComponent, Offeror> pair in _assistant.Offers) {
              pair.Value.Activate();
            }
          }
        }
        else {
          try {
            _assistant.Properties.FailureCallback.OnLoginFailure(_assistant, caught);
          }
          catch (Exception e) {
            Logger.Error("Erro ao executar a callback de falha de login fornecida pelo usuário.", e);
          }
          Thread.Sleep(_assistant.Properties.Interval * 1000);
        }
      }
    }
  }
}