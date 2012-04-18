using System;
using log4net;
using omg.org.CORBA;
using omg.org.PortableInterceptor;
using tecgraf.openbus.core.v2_00.services.access_control;
using tecgraf.openbus.sdk.exceptions;

namespace tecgraf.openbus.sdk.interceptors {
  /// <summary>
  /// Representa o interceptador cliente.
  /// Implementa PortableInterceptor.ClientRequestInterceptor.
  /// </summary>
  internal class ClientInterceptor : InterceptorImpl,
                                             ClientRequestInterceptor {
    #region Fields

    private static readonly ILog Logger =
      LogManager.GetLogger(typeof(ClientInterceptor));

    private static ClientInterceptor _instance;

    #endregion

    #region Contructor

    /// <summary>
    /// Inicializa uma nova inst�ncia de OpenbusAPI.Interceptors.StandardClientInterceptor
    /// </summary>
    private ClientInterceptor()
      : base("ClientInterceptor") {
    }

    internal static ClientInterceptor Instance {
      get { return _instance ?? (_instance = new ClientInterceptor()); }
    }

    #endregion

    #region ClientRequestInterceptor Members

    /// <summary>
    /// Intercepta o request para inser��o de informa��o de contexto.
    /// </summary>
    /// <remarks>Informa��o do cliente</remarks>
    public void send_request(ClientRequestInfo ri) {
      if (!Manager.IsCurrentThreadIgnored()) {
        ConnectionImpl conn = GetCurrentConnection(ri) as ConnectionImpl;
        if (conn != null) {
          conn.SendRequest(ri);
          return;
        }
        Logger.Fatal("Sem conex�o ao barramento, imposs�vel realizar a chamada remota.");
        throw new NO_PERMISSION(NoLoginCode.ConstVal, CompletionStatus.Completed_No);
      }
      Logger.Info("O login est� sendo ignorado para esta chamada.");
    }

    /// <inheritdoc />
    public void receive_exception(ClientRequestInfo ri) {
      ConnectionImpl conn = GetCurrentConnection(ri) as ConnectionImpl;
      if (conn != null) {
        conn.ReceiveException(ri);
        return;
      }
      Logger.Fatal("Sem conex�o ao barramento para receber uma exce��o.");
    }

    #endregion

    #region ClientRequestInterceptor Not Implemented

    /// <inheritdoc />
    public virtual void receive_other(ClientRequestInfo ri) {
      //Nada a ser feito;
    }

    /// <inheritdoc />
    public virtual void receive_reply(ClientRequestInfo ri) {
      //Nada a ser feito;
    }

    /// <inheritdoc />
    public virtual void send_poll(ClientRequestInfo ri) {
      //Nada a ser feito;
    }

    #endregion

    private Connection GetCurrentConnection(RequestInfo ri) {
      try {
        string id = "-1";
        object obj = ri.get_slot(Manager.CurrentThreadSlotId);
        if (obj != null) {
          id = obj.ToString();
        }
        Connection connection = Manager.GetConnectionByThreadId(Convert.ToInt32(id));
        if (connection == null) {
          connection = Manager.DefaultConnection;
          if (connection == null) {
            Logger.Fatal("Imposs�vel retornar conex�o corrente, pois n�o foi definida.");
            throw new NO_PERMISSION(NoLoginCode.ConstVal, CompletionStatus.Completed_No);
          }
          Logger.Debug("Utilizando a conex�o padr�o para realizar a chamada.");
        }
        Logger.Debug("Utilizando a conex�o da thread para realizar a chamada.");
        return connection;
      }
      catch (InvalidSlot e) {
        const string message = "Falha inesperada ao obter o slot da conex�o corrente";
        throw new OpenBusException(message, e);
      }
    }
  }
}