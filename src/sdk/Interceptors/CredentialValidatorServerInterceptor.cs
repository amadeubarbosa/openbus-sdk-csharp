using System;
using log4net;
using omg.org.CORBA;
using omg.org.PortableInterceptor;
using tecgraf.openbus.core.v1_05.access_control_service;


namespace Tecgraf.Openbus.Interceptors
{
  internal class CredentialValidatorServerInterceptor : ServerRequestInterceptor
  {
    #region Fields

    private static ILog logger = LogManager.GetLogger(typeof(CredentialValidatorServerInterceptor));

    #endregion

    #region Constructors

    /// <summary>
    /// Construtor padrão.
    /// </summary>
    public CredentialValidatorServerInterceptor() { }

    #endregion

    #region ServerRequestInterceptor Members

    /// <inheritdoc />
    public void receive_request(ServerRequestInfo ri) {
      Openbus openbus = Openbus.GetInstance();
      //isInterptable
      String interceptedOperation = ri.operation;
      foreach (string methodName in Openbus.CORBA_OBJECT_METHODS) {
        if (interceptedOperation.Equals(methodName)) {
          logger.Debug(String.Format(
              "A operação {0} não deve ser interceptada pelo servidor.", interceptedOperation));
          return;
        }
      }

      Credential interceptedCredential = openbus.GetInterceptedCredential();
      if (String.IsNullOrEmpty(interceptedCredential.identifier)) {
        throw new NO_PERMISSION(100, CompletionStatus.Completed_No);
      }

      IAccessControlService acs = openbus.GetAccessControlService();
      bool isValid = false;

      try {
        isValid = acs.isValid(interceptedCredential);
      }
      catch (AbstractCORBASystemException e) {
        logger.Fatal(String.Format(
          "Erro ao tentar validar a credencial {0}:{1}",
          interceptedCredential.owner, interceptedCredential.identifier), e);
        throw new NO_PERMISSION(10, CompletionStatus.Completed_No);
      }

      if (isValid) {
        logger.Info(String.Format("A credencial {0}:{1} é válida.",
          interceptedCredential.owner, interceptedCredential.identifier));
      }
      else {
        logger.Warn(String.Format("A credencial {0}:{1} não é válida.",
          interceptedCredential.owner, interceptedCredential.identifier));
        throw new NO_PERMISSION(50, CompletionStatus.Completed_No);
      }
    }

    /// <inheritdoc />
    public string Name {
      get { return typeof(CredentialValidatorServerInterceptor).Name; }
    }

    #endregion

    #region ServerRequestInterceptor Not Implemented

    /// <inheritdoc />
    public void receive_request_service_contexts(ServerRequestInfo ri) {
      //Nada a ser feito
    }

    /// <inheritdoc />
    public virtual void send_exception(ServerRequestInfo ri) {
      //Nada a ser feito
    }

    /// <inheritdoc />
    public virtual void send_other(ServerRequestInfo ri) {
      //Nada a ser feito
    }

    /// <inheritdoc />
    public virtual void send_reply(ServerRequestInfo ri) {
      //Nada a ser feito
    }

    #endregion

  }


}
