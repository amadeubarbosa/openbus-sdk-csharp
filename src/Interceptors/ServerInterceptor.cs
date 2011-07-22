using System;
using Ch.Elca.Iiop.Idl;
using omg.org.CORBA;
using omg.org.IOP;
using omg.org.PortableInterceptor;
using OpenbusAPI.Logger;
using tecgraf.openbus.core.v1_05.access_control_service;

namespace OpenbusAPI.Interceptors
{
  internal class ServerInterceptor : InterceptorImpl, ServerRequestInterceptor
  {
    #region Fields

    /// <summary>
    ///O slot para transporte da credencial.
    /// </summary>
    private int credentialSlot;

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
      Log.INTERCEPTORS.Info(String.Format(
        "A operação '{0}' foi interceptada no servidor.", interceptedOperation));

      Openbus openbus = Openbus.GetInstance();
      ServiceContext serviceContext;
      try {
        serviceContext = ri.get_request_service_context(CONTEXT_ID);
      }
      catch (BAD_PARAM) {
        Log.INTERCEPTORS.Warn(String.Format(
          "A chamada à operação '{0}' não possui credencial.", interceptedOperation));
        return;
      }

      if (serviceContext.context_data == null) {
        Log.INTERCEPTORS.Fatal(String.Format(
          "A chamada à operação '{0}' não possui credencial.", interceptedOperation));
        return;
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
        Log.INTERCEPTORS.Fatal("Erro na validação da credencial", e);
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
