using System;
using Ch.Elca.Iiop.Idl;
using log4net;
using omg.org.CORBA;
using omg.org.IOP;
using omg.org.PortableInterceptor;
using tecgraf.openbus.sdk.Interceptors;

namespace tecgraf.openbus.sdk.Standard.Interceptors
{
  internal class StandardServerInterceptor : InterceptorImpl, ServerRequestInterceptor
  {
    #region Fields

    private static readonly ILog Logger = LogManager.GetLogger(typeof(StandardServerInterceptor));

    private readonly StandardOpenbus _bus;
    private readonly StandardConnection _connection;

    #endregion

    #region Constructors

    /// <summary>
    /// Inicializa uma nova instância de OpenbusAPI.Interceptors.StandardServerInterceptor.   
    /// </summary>
    /// <param name="bus">Barramento de uma única conexão.</param>
    /// <param name="codec">Codificador.</param>
    public StandardServerInterceptor(StandardOpenbus bus, Codec codec)
      : base("StandardServerInterceptor", codec) {
      _bus = bus;
      _connection = _bus.Connect() as StandardConnection;
    }

    #endregion

    #region ServerRequestInterceptor Implemented

    /// <inheritdoc />
    public void receive_request_service_contexts(ServerRequestInfo ri) {

      String interceptedOperation = ri.operation;
      Logger.Info(String.Format(
        "A operação '{0}' foi interceptada no servidor.", interceptedOperation));

      sdk.Openbus openbus = sdk.Openbus.GetInstance();
      ServiceContext serviceContext;
      try {
        serviceContext = ri.get_request_service_context(ContextId);
      }
      catch (BAD_PARAM) {
        Logger.Warn(String.Format(
          "A chamada à operação '{0}' não possui credencial.", interceptedOperation));
        return;
      }

      bool isCorbaObjMethod = false;
      foreach (String s in _corbaObjMethods) {
        if (s.Equals(interceptedOperation)) {
          isCorbaObjMethod = true;
          Logger.Info(String.Format("A operação '{0}' é da interface CORBA::OBject e portanto não precisa de credencial.", interceptedOperation));
          break;
        }
      }

      if (!isCorbaObjMethod) {
        if (serviceContext.context_data == null) {
          Logger.Fatal(String.Format(
            "A chamada à operação '{0}' não possui credencial.", interceptedOperation));
          return;
        }
        Logger.Info(String.Format("A operação '{0}' possui credencial.", interceptedOperation));
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
        Logger.Fatal("Erro na validação da credencial", e);
      }
    }

    #endregion

    #region ServerRequestInterceptor Not Implemented

    /// <inheritdoc />
    public virtual void receive_request(ServerRequestInfo ri) {

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
