using System;
using System.Collections.Generic;
using System.Text;
using omg.org.PortableInterceptor;
using tecgraf.openbus.core.v1_06.access_control_service;
using OpenbusAPI.Utils;
using System.Timers;
using OpenbusAPI.Logger;
using omg.org.CORBA;

namespace OpenbusAPI.Interceptors
{
  /// <summary>
  /// Implementa a política de validação de credenciais interceptadas em um servidor,
  /// armazenando as credenciais já validadas em um cache para futuras consultas.
  /// </summary>  
  class CachedCredentialValidatorServerInterceptor : ServerRequestInterceptor
  {
    #region Consts

    /// <summary>
    /// Intervalo de tempo para execução da tarefa de validação das credenciais do 
    /// cache.
    /// </summary>
    private const long TASK_INTERVAL = 5 * 60 * 1000; // 5 minutos
    /// <summary>
    /// Tamanho máximo do cache de credenciais.
    /// </summary>
    private const int MAXIMUM_CREDENTIALS_CACHE_SIZE = 20;

    #endregion

    #region Fields

    /// <summary>
    /// O cache de credenciais que utiliza a política LRU.
    /// </summary>
    private LRUCache<Credential> cache;

    /// <summary>
    /// responsável por agendar a execução da tarefa de validação das 
    /// credenciais do cache.
    /// </summary>
    private Timer timer;

    #endregion

    #region Constructors

    /// <summary>
    /// Construtor.
    /// </summary>
    public CachedCredentialValidatorServerInterceptor() {
      this.cache = new LRUCache<Credential>(MAXIMUM_CREDENTIALS_CACHE_SIZE);
      this.timer = new Timer(TASK_INTERVAL);
      timer.Elapsed += new ElapsedEventHandler(CredentialValidator);     

      Log.INTERCEPTORS.Debug(String.Format(
          "Cache de credenciais com capacidade máxima de {} credenciais.",
          MAXIMUM_CREDENTIALS_CACHE_SIZE));
      Log.INTERCEPTORS.Debug(String.Format(
          "Revalidação do cache realizada a cada {} milisegundos.",
          TASK_INTERVAL));
    }

    #endregion

    #region ServerRequestInterceptor Members

    public void receive_request(ServerRequestInfo ri) {
      Openbus openbus = Openbus.GetInstance();
      //isInterptable

      Credential interceptedCredential = openbus.GetInterceptedCredential();
      if (String.IsNullOrEmpty(interceptedCredential.identifier)) {
        throw new NO_PERMISSION(100, CompletionStatus.Completed_No);
      }

      if (cache.Contains(interceptedCredential)) {
        Log.INTERCEPTORS.Debug(String.Format("A credencial {0}:{1} já está no cache.",
            interceptedCredential.identifier, interceptedCredential.owner));
        return;
      }
      Log.INTERCEPTORS.Debug(String.Format("A credencial {0}:{1} não está no cache.",
          interceptedCredential.identifier, interceptedCredential.owner));

      IAccessControlService acs = openbus.GetAccessControlService();
      bool isValid = false;
      try {
        isValid = acs.isValid(interceptedCredential);
      }
      catch (AbstractCORBASystemException e) {
        Log.INTERCEPTORS.Fatal(String.Format(
          "Erro ao tentar validar a credencial {0}:{1}",
          interceptedCredential.owner, interceptedCredential.identifier), e);
        throw new NO_PERMISSION(10, CompletionStatus.Completed_No);
      }

      if (isValid) {
        Log.INTERCEPTORS.Info(String.Format("A credencial {0}:{1} é válida.",
          interceptedCredential.owner, interceptedCredential.identifier));
        cache.Add(interceptedCredential);
      }
      else {
        Log.INTERCEPTORS.Warn(String.Format("A credencial {0}:{1} não é válida.",
          interceptedCredential.owner, interceptedCredential.identifier));
        throw new NO_PERMISSION(50, CompletionStatus.Completed_No);
      }

    }

    public string Name {
      get { return typeof(CachedCredentialValidatorServerInterceptor).Name; }
    }

    #endregion

    #region Interceptor ServerRequestInterceptor Not Implemented Members

    public void receive_request_service_contexts(ServerRequestInfo ri) {
      //Nada a ser feito
    }

    public void send_exception(ServerRequestInfo ri) {
      //Nada a ser feito
    }

    public void send_other(ServerRequestInfo ri) {
      //Nada a ser feito
    }

    public void send_reply(ServerRequestInfo ri) {
      //Nada a ser feito
    }

    #endregion

    #region Timer

    void CredentialValidator(object sender, ElapsedEventArgs e) {
      Log.INTERCEPTORS.Debug("Executando a tarefa de validação de credenciais.");

      if (cache.Count <= 0) {
        Log.INTERCEPTORS.Debug("Não existem credenciais na cache.");
        return;
      }
      Credential[] credentialArray = new Credential[cache.Count];
      cache.CopyTo(credentialArray, 0);

      Openbus openbus = Openbus.GetInstance();
      IAccessControlService acs = openbus.GetAccessControlService();
      Boolean[] results;
      try {
        results = acs.areValid(credentialArray);
      }
      catch (AbstractCORBASystemException ex) {
        Log.INTERCEPTORS.Error("Erro ao tentar validar as credenciais.", ex);
        return;
      }
      for (int i = 0; i < results.Length; i++) {
        Credential credential = credentialArray[i];
        if (results[i] == false) {
          Log.INTERCEPTORS.Debug(String.Format("A credencial {0}:{1} não é mais válida.",
            credential.identifier, credential.owner));
          cache.Remove(credential);
        }
        else {
          Log.INTERCEPTORS.Debug(String.Format("A credencial {0}:{1} ainda é válida.",
            credential.identifier, credential.owner));
        }
      }
    }

    #endregion
  }
}
