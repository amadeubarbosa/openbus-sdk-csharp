using System;
using System.Collections.Generic;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using Ch.Elca.Iiop;
using Ch.Elca.Iiop.Idl;
using tecgraf.openbus.sdk.Exceptions;
using tecgraf.openbus.sdk.Implementations;
using tecgraf.openbus.sdk.Interceptors;
using log4net;
using omg.org.CORBA;
using scs.core;

namespace tecgraf.openbus.sdk
{
  /// <summary>
  /// API de acesso a um barramento OpenBus.
  /// </summary>
  public class Openbus
  {
    #region Fields

    private static readonly ILog Logger = LogManager.GetLogger(typeof(Openbus));

    /// <summary>
    /// O Orb do IIOP.NET.
    /// <!-- Atenção: O orb é singleton, com isso só podemos registrar 
    /// interceptadores uma única vez. -->
    /// </summary>
    private OrbServices orb;

    /// <summary>
    /// O canal IIOP responsável pela troca de mensagens com o barramento.
    /// </summary>
    private IiopChannel channel;

    /// <summary>
    /// Mantém a lista de métodos a serem liberados por interface
    /// no interceptador servidor. 
    /// </summary>
    private Dictionary<String, List<String>> interceptableMethods;

    #endregion

    #region Consts

    /// <summary>
    /// Representa a chave para obtenção do barramento.
    /// </summary>
    internal const String OPENBUS_KEY = "openbus_v" + OPENBUS_VERSION;

    /// <summary>
    /// Conjunto de Object Keys do Serviço de Controle de Acesso.
    /// </summary>
    internal const String ACCESS_CONTROL_SERVICE_KEY = "ACS_v" + OPENBUS_VERSION;
    internal const String LEASE_PROVIDER_KEY = "LP_v" + OPENBUS_VERSION;
    internal const String FAULT_TOLERANT_ACS_KEY = "FTACS_v" + OPENBUS_VERSION;

    /// <summary>
    /// Object Keys do Serviço de Registro.
    /// </summary>
    internal const String REGISTRY_SERVICE_KEY = "RS_v" + OPENBUS_VERSION;

    /// <summary>
    /// Métodos remotos que podem ser lançados por um objeto CORBA.
    /// </summary>
    internal static readonly String[] CORBA_OBJECT_METHODS = new String[7] {
        "_interface",
        "_is_a",
        "_non_existent", 
        "_domain_managers",
        "_component",
        "_repository_id",
        "_get_policy"
    };
    #endregion

    #region Constructors

    /// <summary>
    /// Construtor da classe Openbus
    /// </summary>
    public Openbus(String host, long port, bool hasServant) {
      // Adiciona interceptadores
      if (this.orb == null) {
        this.orb = OrbServices.GetSingleton();
        this.orb.RegisterPortableInterceptorInitalizer(
            new ClientInitializer());
        this.orb.RegisterPortableInterceptorInitalizer(
            new ServerInitializer(CredentialValidationPolicy.ALWAYS));
        this.orb.CompleteInterceptorRegistration();
      }

      if (hasServant)
        this.channel = new IiopChannel(0);
      else
        this.channel = new IiopChannel();
      ChannelServices.RegisterChannel(channel, false);
    }

    #endregion

    #region Internal Members

    /// <summary>
    /// Se conecta ao AccessControlServer por meio do endereço e da porta.
    /// </summary>
    /// <exception cref="ACSUnavailableException"> Erro ao obter o serviço de 
    /// controle de acesso.</exception>
    internal void FetchACS() {
      this.acsComponent = RemotingServices.Connect(typeof(IComponent),
          "corbaloc::1.0@" + host + ":" + port + "/" + OPENBUS_KEY) as
          IComponent;

      if (this.acsComponent == null) {
        Logger.Error("O serviço de controle de acesso não foi encontrado");
        throw new ACSUnavailableException();
      }

      String acsID = Repository.GetRepositoryID(typeof(IAccessControlService));

      MarshalByRefObject acsObjRef;
      try {
        acsObjRef = this.acsComponent.getFacet(acsID);
      }
      catch (AbstractCORBASystemException e) {
        Logger.Error("O serviço de controle de acesso não foi encontrado", e);
        throw new ACSUnavailableException();
      }

      if (acsObjRef == null) {
        Logger.Error("O serviço de controle de acesso não foi encontrado");
        return;
      }
      this.acs = acsObjRef as IAccessControlService;

      String leaseProviderID = Repository.GetRepositoryID(typeof(ILeaseProvider));
      MarshalByRefObject lpObjRef = this.acsComponent.getFacet(leaseProviderID);
      if (lpObjRef == null) {
        Logger.Error(
          "O serviço de controle de acesso não possui a faceta ILeaseProvider");
        return;
      }
      this.leaseProvider = lpObjRef as ILeaseProvider;
    }

    /// <summary>
    /// Fornece o componente do Serviço de Controle de Acesso.
    /// </summary>
    /// <returns>
    /// A faceta IComponent do Serviço de Controle de Acesso.
    /// </returns>
    internal IComponent GetACSComponent() {
      return this.acsComponent;
    }

    /// <summary>
    /// Fornece a faceta ILeaseProvider do Serviço de Controle de Acesso.
    /// </summary>
    /// <returns>
    /// A faceta ILeaseProvider do Serviço de Controle de Acesso.
    /// </returns>
    internal ILeaseProvider GetLeaseProvider() {
      return this.leaseProvider;
    }

    #endregion

    #region Public Members

    /// <summary>
    /// Inicializa o Orb.
    /// </summary>
    /// <param name="host">O endereço do serviço de controle de acesso.</param>
    /// <param name="port">A porta do serviço de controle de acesso.</param>
    /// <exception cref="ArgumentException">Caso os argumentos estejam
    /// incorretos</exception>
    /// <exception cref="System.Security.SecurityException">Caso não possua
    /// permissão para configurar um canal.</exception>
    public IConnection Connect(String host, long port) {
      return new ConnectionImpl(host, port);
    }

    #endregion
  }
}
