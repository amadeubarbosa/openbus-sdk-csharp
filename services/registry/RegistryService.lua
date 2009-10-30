-- $Id$

local os = os
local table = table
local string = string

local loadfile = loadfile
local assert = assert
local pairs = pairs
local ipairs = ipairs
local error = error
local next = next
local format = string.format

local luuid = require "uuid"
local oil = require "oil"
local orb = oil.orb

local TableDB  = require "openbus.util.TableDB"
local OffersDB = require "core.services.registry.OffersDB"
local Openbus  = require "openbus.Openbus"

local Log = require "openbus.util.Log"
local oop = require "loop.simple"

---
--Componente (membro) respons�vel pelo Servi�o de Registro.
---
module("core.services.registry.RegistryService")

------------------------------------------------------------------------------
-- Faceta IRegistryService
------------------------------------------------------------------------------

RSFacet = oop.class{}

---
--Registra uma nova oferta de servi�o. A oferta de servi�o � representada por
--uma tabela com os campos:
--   properties: lista de propriedades associadas � oferta (opcional)
--               cada propriedade � um par nome/valor (lista de strings)
--   member: refe�rncia para o membro que faz a oferta
--
--@param serviceOffer A oferta de servi�o.
--
--@return true e o identificador do registro da oferta em caso de sucesso, ou
--false caso contr�rio.
---
function RSFacet:register(serviceOffer)
  local identifier = self:generateIdentifier()
  local credential = Openbus:getInterceptedCredential()
  local properties = self:createPropertyIndex(serviceOffer.properties,
    serviceOffer.member)
  local memberName = properties.component_id.name

  -- Recupera todas as facetas do membro
  local allFacets
  local metaInterface = serviceOffer.member:getFacetByName("IMetaInterface")
  if metaInterface then
    metaInterface = orb:narrow(metaInterface, "IDL:scs/core/IMetaInterface:1.0")
    allFacets = metaInterface:getFacets()
  else
    allFacets = {}
    Log:service(format(
      "Membro '%s' (%s) n�o disponibiliza a interface IMetaInterface.",
      memberName, credential.owner))
  end

  local offerEntry = {
    offer = serviceOffer,
  -- Mapeia as propriedades.
    properties = properties,
  -- Mapeia as facetas do componente.
    facets = self:createFacetIndex(credential.owner, memberName, allFacets),
    allFacets = allFacets,
    credential = credential,
    identifier = identifier
  }

  Log:service("Registrando oferta com id "..identifier)

  self:addOffer(offerEntry)
  self.offersDB:insert(offerEntry)

  return true, identifier
end

---
--Adiciona uma oferta ao reposit�rio.
--
--@param offerEntry A oferta.
---
function RSFacet:addOffer(offerEntry)

  -- �ndice de ofertas por identificador
  self.offersByIdentifier[offerEntry.identifier] = offerEntry

  -- �ndice de ofertas por credencial
  local credential = offerEntry.credential
  if not self.offersByCredential[credential.identifier] then
    Log:service("Primeira oferta da credencial "..credential.identifier)
    self.offersByCredential[credential.identifier] = {}
  end
  self.offersByCredential[credential.identifier][offerEntry.identifier] =
    offerEntry

  -- A credencial deve ser observada, porque se for deletada as
  -- ofertas a ela relacionadas devem ser removidas
  local accessControlService = self.context.AccessControlServiceReceptacle
  accessControlService:addCredentialToObserver(self.observerId,
                                               credential.identifier)
  Log:service("Adicionada credencial no observador")
end

---
--Constr�i um conjunto com os valores das propriedades, para acelerar a busca.
--OBS: procedimento v�lido enquanto propriedade for lista de strings !!!
--
--@param offerProperties As propriedades da oferta de servi�o.
--@param member O membro dono das propriedades.
--
--@return As propriedades da oferta em uma tabela cuja chave � o nome da
--propriedade.
---
function RSFacet:createPropertyIndex(offerProperties, member)
  local properties = {}
  for _, property in ipairs(offerProperties) do
    properties[property.name] = {}
    for _, val in ipairs(property.value) do
      properties[property.name][val] = true
    end
  end
  local componentId = member:getComponentId()
  local compId = componentId.name..":"..componentId.major_version.. "."
      .. componentId.minor_version.."."..componentId.patch_version
  properties["component_id"] = {}
  properties["component_id"].name = componentId.name
  properties["component_id"][compId] = true

  local credential = Openbus:getInterceptedCredential()
  properties["registered_by"] = {}
  properties["registered_by"][credential.owner] = true

  return properties
end

---
-- Busca as interfaces por meio da metainterface do membro e as 
-- disponibiza para consulta.
--
-- @param owner Dono da credencial.
-- @param memberName Membro do barramento.
-- @param allFacets Array de facetas do membro.
--
-- @result �ndice de facetas dispon�veis do membro.
--
function RSFacet:createFacetIndex(owner, memberName, allFacets)
  local count = 0
  local facets = {}
  local mgm = self.context.IManagement
  for _, facet in ipairs(allFacets) do
    if mgm:hasAuthorization(owner, facet.interface_name) then
      facets[facet.name] = true
      facets[facet.interface_name] = true
      count = count + 1
    end
  end
  Log:service(format("Membro '%s' (%s) possui %d faceta(s) autorizada(s).",
    memberName, owner, count))
  return facets
end

---
--Remove uma oferta de servi�o.
--
--@param identifier A identifica��o da oferta de servi�o.
--
--@return true caso a oferta tenha sido removida, ou false caso contr�rio.
---
function RSFacet:unregister(identifier)
  Log:service("Removendo oferta "..identifier)

  local offerEntry = self.offersByIdentifier[identifier]
  if not offerEntry then
    Log:warning("Oferta a remover com id "..identifier.." n�o encontrada")
    return false
  end

  local credential = Openbus:getInterceptedCredential()
  if credential.identifier ~= offerEntry.credential.identifier then
    Log:warning("Oferta a remover("..identifier..
                ") n�o registrada com a credencial do chamador")
    return false -- esse tipo de erro merece uma exce��o!
  end

  -- Remove oferta do �ndice por identificador
  self.offersByIdentifier[identifier] = nil

  -- Remove oferta do �ndice por credencial
  local credentialOffers = self.offersByCredential[credential.identifier]
  if credentialOffers then
    credentialOffers[identifier] = nil
  else
    Log:service("N�o h� ofertas a remover com credencial "..
                credential.identifier)
    return true
  end
  if not next (credentialOffers) then
    -- N�o h� mais ofertas associadas � credencial
    self.offersByCredential[credential.identifier] = nil
    Log:service("�ltima oferta da credencial: remove credencial do observador")
    local accessControlService = self.context.AccessControlServiceReceptacle
    accessControlService:removeCredentialFromObserver(self.observerId,
                                                         credential.identifier)
  end
  self.offersDB:delete(offerEntry)
  return true
end

---
--Atualiza a oferta de servi�o associada ao identificador especificado. Apenas
--as propriedades da oferta podem ser atualizadas (nessa vers�o, substituidas).
--
--@param identifier O identificador da oferta.
--@param properties As novas propriedades da oferta.
--
--@return true caso a oferta seja atualizada, ou false caso contr�rio.
---
function RSFacet:update(identifier, properties)
  Log:service("Atualizando oferta "..identifier)

  local offerEntry = self.offersByIdentifier[identifier]
  if not offerEntry then
    Log:warning("Oferta a atualizar com id "..identifier.." n�o encontrada")
    return false
  end

  local credential = Openbus:getInterceptedCredential()
  if credential.identifier ~= offerEntry.credential.identifier then
    Log:warning("Oferta a atualizar("..identifier..
                ") n�o registrada com a credencial do chamador")
    return false -- esse tipo de erro merece uma exce��o!
  end

  -- Atualiza as propriedades da oferta de servi�o
  offerEntry.offer.properties = properties
  offerEntry.properties = self:createPropertyIndex(properties,
                                                   offerEntry.offer.member)
  self.offersDB:update(offerEntry)
  return true
end

---
-- Atualiza as ofertas de facetas do membro.
--
-- @param owner Identifica��o do membro.
--
function RSFacet:updateFacets(owner)
  for id, offerEntry in pairs(self.offersByIdentifier) do
    if offerEntry.credential.owner == owner then
      offerEntry.facets = self:createFacetIndex(owner, 
        offerEntry.properties.component_id.name, offerEntry.allFacets)
      self.offersDB:update(offerEntry)
    end
  end
end

---
--Busca por ofertas de servi�o que implementam as facetas descritas.
--Se nenhuma faceta for fornecida, todas as facetas s�o retornadas.
--
--@param facets As facetas da busca.
--
--@return As ofertas de servi�o que foram encontradas.
---
function RSFacet:find(facets)
  local selectedOffers = {}
-- Se nenhuma faceta foi discriminada, todas as ofertas de servi�o
-- s�o retornadas.
  if (#facets == 0) then
    for _, offerEntry in pairs(self.offersByIdentifier) do
      table.insert(selectedOffers, offerEntry.offer)
    end
  else
  -- Para cada oferta de servi�o dispon�vel, selecionar-se
  -- a oferta que implementa todas as facetas discriminadas.
    for _, offerEntry in pairs(self.offersByIdentifier) do
      local hasAllFacets = true
      for _, facet in ipairs(facets) do
        if not offerEntry.facets[facet] then
          hasAllFacets = false
          break
        end
      end
      if hasAllFacets then
        table.insert(selectedOffers, offerEntry.offer)
      end
    end
     Log:service("Encontrei "..#selectedOffers.." ofertas "..
      "que implementam as facetas discriminadas.")
  end
  return selectedOffers
end

---
--Busca por ofertas de servi�o que implementam as facetas descritas, e,
--que atendam aos crit�rios (propriedades) especificados.
--
--@param facets As facetas da busca.
--@param criteria Os crit�rios da busca.
--
--@return As ofertas de servi�o que foram encontradas.
---
function RSFacet:findByCriteria(facets, criteria)
  local selectedOffers = {}
-- Se nenhuma faceta foi discriminada e nenhum crit�rio foi
-- definido, todas as ofertas de servi�o s�o retornadas.
  if (#facets == 0 and #criteria == 0) then
    for _, offerEntry in pairs(self.offersByIdentifier) do
      table.insert(selectedOffers, offerEntry.offer)
    end
  else
-- Para cada oferta de servi�o dispon�vel, seleciona-se
-- a oferta que implementa todas as facetas discriminadas,
-- e, possui todos os crit�rios especificados.
    for _, offerEntry in pairs(self.offersByIdentifier) do
      if self:meetsCriteria(criteria, offerEntry.properties) then
        local hasAllFacets = true
        for _, facet in ipairs(facets) do
          if not offerEntry.facets[facet] then
            hasAllFacets = false
            break
          end
        end
        if hasAllFacets then
          table.insert(selectedOffers, offerEntry.offer)
        end
      end
    end
     Log:service("Com crit�rio, encontrei "..#selectedOffers.." ofertas "..
      "que implementam as facetas discriminadas.")
  end
  return selectedOffers
end

---
--Verifica se uma oferta atende aos crit�rios de busca
--
--@param criteria Os crit�rios da busca.
--@param offerProperties As propriedades da oferta.
--
--@return true caso a oferta atenda aos crit�rios, ou false caso contr�rio.
---
function RSFacet:meetsCriteria(criteria, offerProperties)
  for _, criterion in ipairs(criteria) do
    local offerProperty = offerProperties[criterion.name]
    if offerProperty then
      for _, val in ipairs(criterion.value) do
        if not offerProperty[val] then
          return false -- oferta n�o tem valor em seu conjunto
        end
      end
    else
      return false -- oferta n�o tem propriedade com esse nome
    end
  end
  return true
end

---
--Notifica��o de dele��o de credencial. As ofertas de servi�o relacionadas
--dever�o ser removidas.
--
--@param credential A credencial removida.
---
function RSFacet:credentialWasDeleted(credential)
  Log:service("Remover ofertas da credencial deletada "..credential.identifier)
  local credentialOffers = self.offersByCredential[credential.identifier]
  self.offersByCredential[credential.identifier] = nil

  if credentialOffers then
    for identifier, offerEntry in pairs(credentialOffers) do
      self.offersByIdentifier[identifier] = nil
      Log:service("Removida oferta "..identifier.." do �ndice por id")
      self.offersDB:delete(offerEntry)
    end
  else
    Log:service("N�o havia ofertas da credencial "..credential.identifier)
  end
end

---
--Procedimento ap�s reconex�o do servi�o.
---
function RSFacet:expired()
  Openbus:connectByCertificate(self.context._componentId.name,
      self.privateKeyFile, self.accessControlServiceCertificateFile)

  -- atualiza a refer�ncia junto ao servi�o de controle de acesso
  local accessControlService = self.context.AccessControlServiceReceptacle
  accessControlService:setRegistryService(self)

  -- registra novamente o observador de credenciais
  self.observerId = accessControlService:addObserver(self.observer, {})
  Log:service("Observador recadastrado")

  -- Mant�m no reposit�rio apenas ofertas com credenciais v�lidas
  local offerEntries = self.offersByIdentifier
  local credentials = {}
  for _, offerEntry in pairs(offerEntries) do
    credentials[offerEntry.credential.identifier] = offerEntry.credential
  end
  local invalidCredentials = {}
  for credentialId, credential in pairs(credentials) do
    if not accessControlService:addCredentialToObserver(self.observerId,
                                                            credentialId) then
      Log:service("Ofertas de "..credentialId.." ser�o removidas")
      table.insert(invalidCredentials, credential)
    else
      Log:service("Ofertas de "..credentialId.." ser�o mantidas")
    end
  end
  for _, credential in ipairs(invalidCredentials) do
    self:credentialWasDeleted(credential)
  end

  Log:service("Servi�o de registro foi reconectado")
end

---
--Gera uma identifica��o de oferta de servi�o.
--
--@return O identificador de oferta de servi�o.
---
function RSFacet:generateIdentifier()
    return luuid.new("time")
end

--------------------------------------------------------------------------------
-- Faceta IComponent
--------------------------------------------------------------------------------

---
--Inicia o servico.
--
--@see scs.core.IComponent#startup
---
function startup(self)
  Log:service("Pedido de startup para servi�o de registro")
  local DATA_DIR = os.getenv("OPENBUS_DATADIR")

  local mgm = self.context.IManagement
  local rs = self.context.IRegistryService
  local config = rs.config

  -- Verifica se � o primeiro startup
  if not rs.initialized then
    Log:service("Servi�o de registro est� inicializando")
    if string.match(config.privateKeyFile, "^/") then
      rs.privateKeyFile = config.privateKeyFile
    else
      rs.privateKeyFile = DATA_DIR.."/"..config.privateKeyFile
    end
    if string.match(config.accessControlServiceCertificateFile, "^/") then
      rs.accessControlServiceCertificateFile =
        config.accessControlServiceCertificateFile
    else
      rs.accessControlServiceCertificateFile = DATA_DIR .. "/" ..
        config.accessControlServiceCertificateFile
    end

    -- instancia mecanismo de persistencia
    local databaseDirectory
    if string.match(config.databaseDirectory, "^/") then
      databaseDirectory = config.databaseDirectory
    else
      databaseDirectory = DATA_DIR.."/"..config.databaseDirectory
    end
    rs.offersDB = OffersDB(databaseDirectory)
    rs.initialized = true
  else
    Log:service("Servi�o de registro j� foi inicializado")
  end

  -- Inicializa o reposit�rio de ofertas
  rs.offersByIdentifier = {}   -- id -> oferta
  rs.offersByCredential = {}  -- credencial -> id -> oferta

  -- autentica o servi�o, conectando-o ao barramento
  if not Openbus:isConnected() then
    Openbus:connectByCertificate(self.context._componentId.name,
      rs.privateKeyFile, rs.accessControlServiceCertificateFile)
  end

  -- Cadastra callback para LeaseExpired
  Openbus:addLeaseExpiredCallback( rs )

  -- obt�m a refer�ncia para o Servi�o de Controle de Acesso
  local accessControlService = Openbus:getAccessControlService()
  if not accessControlService then
    error{"IDL:SCS/StartupFailed:1.0"}
  end
  local acsIComp = accessControlService:_component()
  acsIComp = orb:narrow(acsIComp, "IDL:scs/core/IComponent:1.0")

  -- conecta-se ao controle de acesso:   [ACS]--( 0--[RS]
  local acsIReceptacles =  acsIComp:getFacetByName("IReceptacles")
  acsIReceptacles = orb:narrow(acsIReceptacles, "IDL:scs/core/IReceptacles:1.0")
  local success, conId =
    oil.pcall(acsIReceptacles.connect, acsIReceptacles,
              "RegistryServiceReceptacle", self.context.IRegistryService )
  if not success then
    Log:error("Erro durante conex�o do servi�o ao Controle de Acesso.")
    Log:error(conId)
    error{"IDL:SCS/StartupFailed:1.0"}
  end

  -- conecta-se com o controle de acesso:   [RS]--( 0--[ACS]
  local acsFacet = acsIComp:getFacetByName("IAccessControlService")
  acsFacet = orb:narrow(acsFacet,"IDL:openbusidl/acs/IAccessControlService:1.0")
  success, conId =
    oil.pcall(self.context.IReceptacles.connect, self.context.IReceptacles,
              "AccessControlServiceReceptacle", acsFacet)
  if not success then
    Log:error("Erro durante conex�o com servi�o de Controle de Acesso.")
    Log:error(conId)
    error{"IDL:SCS/StartupFailed:1.0"}
  end

  -- registra um observador de credenciais
  local observer = {
    registryService = rs,
    credentialWasDeleted = function(self, credential)
      Log:service("Observador notificado para credencial "..
                  credential.identifier)
      self.registryService:credentialWasDeleted(credential)
    end
  }
  rs.observer = orb:newservant(observer, "RegistryServiceCredentialObserver",
    "IDL:openbusidl/acs/ICredentialObserver:1.0"
  )
  rs.observerId =
    acsFacet:addObserver(rs.observer, {})
  Log:service("Cadastrado observador para a credencial")

  -- recupera ofertas persistidas
  Log:service("Recuperando ofertas persistidas")
  local offerEntriesDB = rs.offersDB:retrieveAll()
  for _, offerEntry in pairs(offerEntriesDB) do
    -- somente recupera ofertas de credenciais v�lidas
    if acsFacet:isValid(offerEntry.credential) then
      rs:addOffer(offerEntry)
    else
      Log:service("Oferta de "..offerEntry.credential.identifier.." descartada")
      rs.offersDB:delete(offerEntry)
    end
  end

  -- Refer�ncia � faceta de gerenciamento do ACS
  mgm.acsmgm = acsIComp:getFacetByName("IManagement")
  mgm.acsmgm = orb:narrow(mgm.acsmgm, "IDL:openbusidl/acs/IManagement:1.0")
  -- Administradores dos servi�os
  mgm.admins = {}
  for _, name in ipairs(config.administrators) do
     mgm.admins[name] = true
  end
  -- ACS � sempre administrador
  mgm.admins.AccessControlService = true
  -- Inicializa a base de gerenciamento
  mgm.authDB = TableDB(DATA_DIR.."/rs_auth.db")
  mgm.ifaceDB = TableDB(DATA_DIR.."/rs_iface.db")
  mgm:loadData()

  rs.started = true
  self.context.IFaultTolerantService:setStatus(true)
  
  Log:service("Servi�o de registro iniciado")
end

---
--Finaliza o servi�o.
--
--@see scs.core.IComponent#shutdown
---
function shutdown(self)
  Log:service("Pedido de shutdown para servi�o de registro")
  local rs = self.context.IRegistryService
  if not rs.started then
    Log:error("Servico ja foi finalizado.")
    error{"IDL:SCS/ShutdownFailed:1.0"}
  end
  rs.started = false

  -- Remove o observador
  if rs.observerId then
    self.context.AccessControlServiceReceptacle:removeObserver(rs.observerId)
    rs.observer:_deactivate()
  end

  if Openbus:isConnected() then
    Openbus:disconnect()
  end

  Log:service("Servi�o de registro finalizado")
  
   orb:deactivate(rs)
   orb:deactivate(self.context.IManagement)
   orb:shutdown()
   Log:faulttolerance("Servico de Registro matou seu processo.")
end

--------------------------------------------------------------------------------
-- Faceta IManagement
--------------------------------------------------------------------------------

ManagementFacet = oop.class{}

---
-- Verifica se o usu�rio tem permiss�o para executar o m�todo.
--
function ManagementFacet:checkPermission()
  local credential = Openbus:getInterceptedCredential()
  local admin = self.admins[credential.owner] or
                self.admins[credential.delegate]
  if not admin then
    error(Openbus:getORB():newexcept {
      "IDL:omg.org/CORBA/NO_PERMISSION:1.0",
      minor_code_value = 0,
      completion_status = 1,
    })
  end
end

---
-- Carrega os dados das bases de dados.
--
function ManagementFacet:loadData()
  -- Cache de objetos
  self.interfaces = {}
  self.authorizations = {}
  -- Carrega interfaces
  local data = assert(self.ifaceDB:getValues())
  for _, iface in ipairs(data) do
    self.interfaces[iface] = true
  end
  -- Carrega as autoriza��es.
  -- Verificar junto ao ACS se as implanta��es ainda existem.
  local remove = {}
  data = assert(self.authDB:getValues())
  for _, auth in ipairs(data) do
    local succ, depl = self.acsmgm.__try:getSystemDeployment(auth.deploymentId)
    if not succ then
      if depl[1] == "IDL:openbusidl/acs/SystemDeploymentNonExistent:1.0" then
        remove[auth] = true
        Log:warn(format("Removendo autoriza��es de '%s': " ..
          "removida do Servi�o de Controle de Acesso.", auth.deploymentId))
      else
        error(depl)  -- Exce��o desconhecida, repassando
      end
    elseif depl.systemId ~= auth.systemId then
      remove[auth] = true
      Log:warn(format("Removendo autoriza��es de '%s': " ..
        " identificador de sistema difere.", auth.deploymentId))
    else
      self.authorizations[auth.deploymentId] = auth
    end
  end
  for auth in pairs(remove) do
    self.authDB:remove(auth.deploymentId)
  end
end

---
-- Cadastra um identificador de interface aceito pelo Servi�o de Registro.
--
-- @param ifaceId Identificador de interface.
--
function ManagementFacet:addInterfaceIdentifier(ifaceId)
  self:checkPermission()
  if self.interfaces[ifaceId] then
    Log:error(format("Interface '%s' j� cadastrada.", ifaceId))
    error{"IDL:openbusidl/rs/InterfaceIdentifierAlreadyExists:1.0"}
  end
  self.interfaces[ifaceId] = true
  local succ, msg = self.ifaceDB:save(ifaceId, ifaceId)
  if not succ then
    Log:error(format("Falha ao salvar a interface '%s': %s",
      ifaceId, msg))
  end
end

---
-- Remove o identificador.
-- 
-- @param ifaceId Identificador de interface.
-- 
function ManagementFacet:removeInterfaceIdentifier(ifaceId)
  self:checkPermission()
  if not self.interfaces[ifaceId] then
    Log:error(format("Interface '%s' n�o est� cadastrada.", ifaceId))
    error{"IDL:openbusidl/rs/InterfaceIdentifierNonExistent:1.0"}
  end
  for _, auth in ipairs(self.authorizations) do
    if auth.authorized[ifaceId] then
      Log:error(format("Interface '%s' em uso.", ifaceId))
      error{"IDL:openbusidl/rs/InterfaceIdentifierInUse:1.0"}
    end
  end
  self.interfaces[ifaceId] = nil
  local succ, msg = self.ifaceDB:remove(ifaceId)
  if not succ then
    Log:error(format("Falha ao remover interface '%s': %s", iface, msg))
  end
end

---
-- Recupera todos os identificadores de interface cadastrados.
--
-- @return Seq��ncia de identificadores de interface.
--
function ManagementFacet:getInterfaceIdentifiers()
  local array = {}
  for iface in pairs(self.interfaces) do
    array[#array+1] = iface
  end
  return array
end

---
-- Autoriza a implanta��o a exportar a interface.  O Servi�o de Acesso
-- � consultado para verificar se a implanta��o est� cadastrada.
--
-- @param deploymentId Identificador da implanta��o.
-- @param ifaceId Identificador da interface.
--
function ManagementFacet:grant(deploymentId, ifaceId)
  self:checkPermission()
  if not self.interfaces[ifaceId] then
    Log:error(format("Interface '%s' n�o cadastrada.", ifaceId))
    error{"IDL:openbusidl/rs/InterfaceIdentifierNonExistent:1.0"}
  end
  local auth = self.authorizations[deploymentId]
  if not auth then 
    -- Cria uma nova autoriza��o: verificar junto ao ACS se implanta��o existe.
    local succ, depl = self.acsmgm.__try:getSystemDeployment(deploymentId)
    if not succ then
      Log:error(format("Implementa��o '%s' n�o cadastrada.",
        deploymentId))
      error{"IDL:openbusidl/rs/SystemDeploymentNonExistent:1.0"}
    end
    auth = {
      deploymentId = deploymentId,
      systemId = depl.systemId,
      authorized = {},
    }
    self.authorizations[deploymentId] = auth
  elseif auth and auth.authorized[ifaceId] then
    return
  end
  auth.authorized[ifaceId] = true
  local succ, msg = self.authDB:save(deploymentId, auth)
  if not succ then
    Log:error(format("Falha ao salvar autoriza��o '%s': %s",
      deploymentId, msg))
  end
  self.context.IRegistryService:updateFacets(deploymentId)
end

---
-- Revoga a autoriza��o para exportar a interface.
--
-- @param deploymentId Identificador da implanta��o.
-- @param ifaceId Identificador da interface.
--
function ManagementFacet:revoke(deploymentId, ifaceId)
  self:checkPermission()
  local auth = self.authorizations[deploymentId]
  if not auth then
    Log:error(format("N�o h� autoriza��o para '%s'.", deploymentId))
    error{"IDL:openbusidl/rs/AuthorizationNonExistent:1.0"}
  elseif not self.interfaces[ifaceId] then
    Log:error(format("Interface '%s' n�o cadastrada.", ifaceId))
    error{"IDL:openbusidl/rs/InterfaceIdentifierNonExistent:1.0"}
  elseif auth.authorized[ifaceId] then
    local succ, msg
    auth.authorized[ifaceId] = nil
    -- Se n�o houver mais autoriza��es, remover a entrada
    if next(auth.authorized) then
      succ, msg = self.authDB:save(deploymentId, auth)
    else
      self.authorizations[deploymentId] = nil
      succ, msg = self.authDB:remove(deploymentId)
    end
    if not succ then
      Log:error(format("Falha ao remover autoriza��o '%s': %s",
        deploymentId, msg))
    end
    self.context.IRegistryService:updateFacets(deploymentId)
  end
end

---
-- Remove a autoriza��o da implanta��o.
--
-- @param deploymentId Identificador da implanta��o.
--
function ManagementFacet:removeAuthorization(deploymentId)
  self:checkPermission()
  if not self.authorizations[deploymentId] then
    Log:error(format("N�o h� autoriza��o para '%s'.", deploymentId))
    error{"IDL:openbusidl/rs/AuthorizationNonExistent:1.0"}
  else
    self.authorizations[deploymentId] = nil
    local succ, msg = self.authDB:remove(deploymentId)
    if not succ then
      Log:error(format("Falha ao remover autoriza��o '%s': %s",
        deploymentId, msg))
    end
    self.context.IRegistryService:updateFacets(deploymentId)
  end
end

---
-- Duplica a autoriza��o, mas a lista de interfaces � retornada
-- como array e n�o como hash. Essa fun��o � usada para exportar
-- a autoriza��o.
--
-- @param auth Autoriza��o a ser duplicada.
-- @return C�pia da autoriza��o.
--
function ManagementFacet:copyAuthorization(auth)
  local tmp = {}
  for k, v in pairs(auth) do
    tmp[k] = v
  end
  -- Muda de hash para array
  local authorized = {}
  for iface in pairs(tmp.authorized) do
    authorized[#authorized+1] = iface
  end
  tmp.authorized = authorized
  return tmp
end

---
-- Verifica se a implanta��o � autorizada a exporta 
-- uma determinada interface.
--
-- @param deploymentId Identificador da implanta��o.
-- @param iface Interface a ser consultada (repID).
--
-- @return true se � autorizada, false caso contr�rio.
--
function ManagementFacet:hasAuthorization(deploymentId, iface)
  local auth = self.authorizations[deploymentId]
  return ((auth and auth.authorized[iface]) and true) or false
end

---
-- Recupera a autoriza��o de uma implanta��o.
--
-- @param deploymentId Identificador da implanta��o.
--
-- @return Autoriza��o da implanta��o.
--
function ManagementFacet:getAuthorization(deploymentId)
  local auth = self.authorizations[deploymentId]
  if not auth then
    Log:error(format("N�o h� autoriza��o para '%s'.", deploymentId))
    error{"IDL:openbusidl/rs/AuthorizationNonExistent:1.0"}
  end
  return self:copyAuthorization(auth)
end

---
-- Recupera todas as autoriza��es cadastradas.
--
-- @return Seq��ncia de autoriza��es.
--
function ManagementFacet:getAuthorizations()
  local array = {}
  for _, auth in pairs(self.authorizations) do
    array[#array+1] = self:copyAuthorization(auth)
  end
  return array
end

---
-- Recupera as autoriza��es das implanta��es de um dado sistema.
--
-- @param systemId Identificador do sistema.
--
-- @return Seq��ncia de autoriza��es.
--
function ManagementFacet:getAuthorizationsBySystemId(systemId)
  local array = {}
  for _, auth in pairs(self.authorizations) do
    if systemId == auth.systemId then
      array[#array+1] = self:copyAuthorization(auth)
    end
  end
  return array
end

---
-- Recupera as autoriza��es que cont�m \e todas as interfaces
-- fornecidas em seu conjunto de interfaces autorizadas.
--
-- @param systemId Identificador do sistema.
--
-- @return Seq��ncia de autoriza��es.
--
function ManagementFacet:getAuthorizationsByInterfaceId(ifaceIds)
  local array = {}
  for _, auth in pairs(self.authorizations) do
    local found = true
    for _, iface in ipairs(ifaceIds) do
      if not auth.authorized[iface] then
        found = false
        break
      end
    end
    if found then
      array[#array+1] = self:copyAuthorization(auth)
    end
  end
  return array
end
