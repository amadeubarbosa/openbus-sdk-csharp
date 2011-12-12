using System;
using Ch.Elca.Iiop.Idl;
using log4net;
using omg.org.CORBA;
using omg.org.IOP;
using omg.org.PortableInterceptor;
using tecgraf.openbus.core.v1_05.access_control_service;


namespace Tecgraf.Openbus.Interceptors
{
  internal class ServerInterceptor : InterceptorImpl, ServerRequestInterceptor
  {
    #region Fields

    private static ILog logger = LogManager.GetLogger(typeof(ServerInterceptor));

    /// <summary>
    ///O slot para transporte da credencial.
    /// </summary>
    private int credentialSlot;

    private String[] corbaObjMethods = new String[] {"_interface", "_is_a", 
      "_non_existent", "_repository_id", "_component", "_domain_managers"};

    #endregion

    #region Constructors


    /// <summary>
    /// Inicializa uma nova instância de OpenbusAPI.Interceptors.ServerInterceptor.   
    /// </summary>
    /// <param name="codec">Codificador.</param>
    /// <param name="credentialSlot">O slot para transporte da credencial.</param>
    public ServerInterceptor(Codec codec, int credentialSlot)
      : base("ServerInterceptor", codec) {
      Openbus openbus = Openbus.GetInstance();
      this.credentialSlot = credentialSlot;
      openbus.RequestCredentialSlot = credentialSlot;
    }

    #endregion

    #region ServerRequestInterceptor Implemented

    /// <inheritdoc />
    public void receive_request_service_contexts(ServerRequestInfo ri) {

      String interceptedOperation = ri.operation;
      logger.Info(String.Format(
        "A operação '{0}' foi interceptada no servidor.", interceptedOperation));

      Openbus openbus = Openbus.GetInstance();
      ServiceContext serviceContext;
      try {
        serviceContext = ri.get_request_service_context(CONTEXT_ID);
      }
      catch (BAD_PARAM) {
        logger.Warn(String.Format(
          "A chamada à operação '{0}' não possui credencial.", interceptedOperation));
        return;
      }

      bool isCorbaObjMethod = false;
      foreach (String s in corbaObjMethods) {
        if (s.Equals(interceptedOperation)) {
          isCorbaObjMethod = true;
          logger.Info(String.Format("A operação '{0}' é da interface CORBA::OBject e portanto não precisa de credencial.", interceptedOperation));
          break;
        }
      }

      if (!isCorbaObjMethod) {
        if (serviceContext.context_data == null) {
          logger.Fatal(String.Format(
            "A chamada à operação '{0}' não possui credencial.", interceptedOperation));
          return;
        }
        logger.Info(String.Format("A operação '{0}' possui credencial.", interceptedOperation));
      }

      try {
        OrbServices orb = OrbServices.GetSingleton();
        Type credentialType = typeof(Credential);
        String credentialTypeName = typeof(Credential).Name;
        String credentialRepId = Repository.GetRepositoryID(credentialType);
        omg.org.CORBA.TypeCode credentialTypeCode =
            orb.create_interface_tc(credentialRepId, credentialTypeName);

        byte[] data = serviceContext.context_data;
        Credential requestCredential = (Credential)
            this.Codec.decode_value(data, credentialTypeCode);

        int openbusRequestCredentialSlot = this.credentialSlot;
        ri.set_slot(openbusRequestCredentialSlot, requestCredential);
      }
      catch (System.Exception e) {
        logger.Fatal("Erro na validação da credencial", e);
      }
    }

    #endregion

    #region ServerRequestInterceptor Not Implemented

    /// <inheritdoc />
    public virtual void receive_request(ServerRequestInfo ri) {
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
