﻿using log4net;
using omg.org.CORBA;
using omg.org.PortableInterceptor;
using tecgraf.openbus.core.v2_00.services.access_control;
using tecgraf.openbus.sdk.Interceptors;

namespace tecgraf.openbus.sdk.Standard.Interceptors {
  internal class StandardServerInterceptor : InterceptorImpl,
                                             ServerRequestInterceptor {
    #region Fields

    private static readonly ILog Logger =
      LogManager.GetLogger(typeof (StandardServerInterceptor));

    private static StandardServerInterceptor _instance;

    #endregion

    #region Constructors

    /// <summary>
    /// Inicializa uma nova instância de OpenbusAPI.Interceptors.StandardServerInterceptor.   
    /// </summary>
    private StandardServerInterceptor()
      : base("StandardServerInterceptor") {
    }

    #endregion

    internal StandardConnection Connection { get; set; }

    internal static StandardServerInterceptor Instance {
      get { return _instance ?? (_instance = new StandardServerInterceptor()); }
    }

    #region ServerRequestInterceptor Implemented

    /// <inheritdoc />
    public void receive_request(ServerRequestInfo ri) {
      if (Connection != null) {
        Connection.ReceiveRequest(ri);
        return;
      }
      Logger.Fatal(
        "Sem conexão ao barramento, impossível receber a chamada remota.");
      throw new NO_PERMISSION(UnverifiedLoginCode.ConstVal,
                              CompletionStatus.Completed_No);
    }

    /// <inheritdoc />
    public void send_exception(ServerRequestInfo ri) {
      if (Connection != null) {
        Connection.SendException(ri);
        return;
      }
      Logger.Fatal(
        "Sem conexão ao barramento, impossível enviar exceção à chamada remota.");
      throw new NO_PERMISSION(UnverifiedLoginCode.ConstVal,
                              CompletionStatus.Completed_No);
    }

    /// <inheritdoc />
    public virtual void send_reply(ServerRequestInfo ri) {
      if (Connection != null) {
        Connection.SendReply(ri);
        return;
      }
      Logger.Fatal(
        "Sem conexão ao barramento, impossível enviar resposta à chamada remota.");
      throw new NO_PERMISSION(UnverifiedLoginCode.ConstVal,
                              CompletionStatus.Completed_No);
    }

    #endregion

    #region ServerRequestInterceptor Not Implemented

    /// <inheritdoc />
    public virtual void receive_request_service_contexts(ServerRequestInfo ri) {
      //Nada a ser feito
    }

    /// <inheritdoc />
    public virtual void send_other(ServerRequestInfo ri) {
      //Nada a ser feito
    }

    #endregion
                                             }
}