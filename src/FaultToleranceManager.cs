using System;
using System.IO;
using System.Xml;
using Tecgraf.Openbus.Exception;
using Tecgraf.Openbus.Logger;

namespace Tecgraf.Openbus
{
  /// <summary>
  /// Representa o controlador das réplicas para tolerância a falhas.
  /// </summary>
  class FaultToleranceManager
  {
    #region Fields

    /// <summary>
    /// A Lista de endereços dos Serviços de Controle de Acesso.
    /// </summary>
    private XmlNodeList acsHostList;

    /// <summary>
    /// O último índice da lista 'acsHostList' onde foi encontrado um 
    /// serviço válido.
    /// </summary>
    /// <seealso cref="acsHostList"/>
    private int lastIndexList;

    /// <summary>
    /// Número de vezes que tentaremos se conectar com as diferentes réplicas.
    /// </summary>
    private int maxTrials;

    #endregion

    #region Consts

    /// <summary>
    /// O nome do arquivo de configuração do Tolerância a falha.
    /// </summary>
    private const String FTCONFIG_FILENAME = "FaultToleranceConfiguration.config";

    /// <summary>
    /// Valor padrão para 'maxTrials'
    /// </summary>
    private const int DEFAULT_MAX_TRIALS = 1;

    /// <summary>
    /// O número de vezes que tentaremos se conectar com as diferentes réplicas.
    /// </summary>
    private const String MAX_TRIALS = "totalTrials";

    /// <summary>
    /// O nome do elemento que contém os endereços do serviço.
    /// </summary>
    private const String HOSTS_ELEMENT_NAME = "hosts";

    /// <summary>
    /// O nome do elemento que contém o endereço do serviço.
    /// </summary>
    private const String HOST_ELEMENT = "host";

    /// <summary>
    /// O nome do atributo que contém a porta do serviço.
    /// </summary>
    private const String PORT_ATTRIBUTE = "port";


    #endregion

    #region Constructors

    /// <summary>
    /// Construtor
    /// </summary>
    /// <param name="ftConfigPath">O caminho do arquivo de configuração.</param>
    public FaultToleranceManager(String ftConfigPath) {
      XmlDocument xmlDocument = new XmlDocument();
      try {
        xmlDocument.Load(ftConfigPath);
      }
      catch (FileNotFoundException) {
        Log.FAULT_TOLERANCE.Fatal("O arquivo '" + ftConfigPath + "' que" +
          "configura o tolerância a falha não foi encontrado ou não existe");
        throw new FileNotFoundException();
      }
      this.acsHostList = xmlDocument.GetElementsByTagName(HOSTS_ELEMENT_NAME);
      this.lastIndexList = 0;

      XmlNodeList trialsNode = xmlDocument.GetElementsByTagName(MAX_TRIALS);
      this.maxTrials = trialsNode.Count > 0 ?
          trialsNode.Count : DEFAULT_MAX_TRIALS;
    }

    /// <summary>
    /// Construtor
    /// </summary>
    public FaultToleranceManager() : this(FTCONFIG_FILENAME) { }

    #endregion

    /// <summary>
    /// Atualiza o estado do Openbus.
    /// 
    /// Este método tenta se conectar com as diferentes réplicas informadas
    /// pelo arquivo de configuração.
    /// </summary>    
    /// <param name="openbus">
    /// A instância do Openbus que deve ser atualizado.
    /// </param>
    /// <returns> <code>true</code> caso a atualização feita com sucesso, ou 
    /// <code>false</code> caso contrário. </returns>
    public bool UpdateOpenbus(Openbus openbus) {
      int indexList = this.lastIndexList;
      int trials = 0;
      bool result = false;

      while ((indexList != this.lastIndexList) || (trials < this.maxTrials)) {
        indexList = (indexList + 1) % this.acsHostList.Count;
        if (indexList == this.lastIndexList)
          trials++;

        XmlNode node = acsHostList.Item(indexList);
        String host = node.Value;
        String portValue = node.Attributes[PORT_ATTRIBUTE].Value;
        int port;

        try {
          port = Convert.ToInt32(portValue);
        }
        catch (FormatException) {
          Log.FAULT_TOLERANCE.Error("Porta inválida para o host '" + host + "'");
          continue;
        }

        openbus.SetACSAdress(host, port);
        try {
          openbus.FetchACS();
        }
        catch (ACSUnavailableException) {
          Log.FAULT_TOLERANCE.Error("Réplica '" + host + ":" + port +
              " não se encontra disponível");
          continue;
        }
        //Testar se serviço está OK?!
        //OrbServices orb = OrbServices.GetSingleton();
        //bool nonExistant = orb.non_existent(proxy);

        this.lastIndexList = indexList;
        result = true;
        break;
      }

      return result;
    }
  }
}