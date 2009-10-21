-- $Id$

local os     = os
local string = string
local table  = table
local math   = math

local loadfile = loadfile
local assert = assert
local pairs = pairs
local ipairs = ipairs
local string = string
local tostring = tostring
local print = print
local error = error
local format = string.format

local luuid = require "uuid"
local lce = require "lce"
local oil = require "oil"
local Openbus = require "openbus.Openbus"

local LeaseProvider = require "openbus.lease.LeaseProvider"

local TableDB       = require "openbus.util.TableDB"
local CredentialDB  = require "core.services.accesscontrol.CredentialDB"
local CertificateDB = require "core.services.accesscontrol.CertificateDB"

local Log = require "openbus.util.Log"

local scs = require "scs.core.base"

local oop = require "loop.simple"


---
--Componente respons�vel pelo Servi�o de Controle de Acesso
---
module("core.services.accesscontrol.AccessControlService")

--------------------------------------------------------------------------------
-- Faceta IAccessControlService
--------------------------------------------------------------------------------

local DATA_DIR = os.getenv("OPENBUS_DATADIR")

ACSFacet = oop.class{}

---
--Credencial inv�lida.
--
--@class table
--@name invalidCredential
--
--@field identifier O identificador da credencial que, neste caso, � vazio.
--@field owner O nome da entidade dona da credencial que, neste caso, � vazio.
--@field delegate O nome da entidade delegada que, neste caso, � vazio.
---
ACSFacet.invalidCredential = {identifier = "", owner = "", delegate = ""}
ACSFacet.invalidLease = -1
ACSFacet.deltaT = 30 -- lease fixo (por enquanto) em segundos
ACSFacet.faultDescription = {_isAlive = false, _errorMsg = "" }
---
--Realiza um login de uma entidade atrav�s de usu�rio e senha.
--
--@param name O nome da entidade.
--@param password A senha da entidade.
--
--@return true, a credencial da entidade e o lease caso o login seja realizado
--com sucesso, ou false e uma credencial e uma lease inv�lidos, caso contr�rio.
---
function ACSFacet:loginByPassword(name, password)
  for _, validator in ipairs(self.loginPasswordValidators) do
    local result, err = validator:validate(name, password)
    if result then
      local entry = self:addEntry(name)
      return true, entry.credential, entry.lease.duration
    else
       Log:warn(format("Erro ao validar o usu�rio %s: %s", name, err))
    end
  end
  Log:error("Usu�rio "..name.." n�o p�de ser validado no sistema.")
  return false, self.invalidCredential, self.invalidLease
end

---
--Realiza um login de um membro atrav�s de assinatura digital.
--
--@param name OI nome do membro.
--@param answer A resposta para um desafio previamente obtido.
--
--@return true, a credencial do membro e o lease caso o login seja realizado
--com sucesso, ou false e uma credencial e uma lease inv�lidos, caso contr�rio.
--
--@see getChallenge
---
function ACSFacet:loginByCertificate(name, answer)
  local challenge = self.challenges[name]
  if not challenge then
    Log:error("Nao existe desafio para "..name)
    return false, self.invalidCredential, self.invalidLease
  end
  local errorMessage
  answer, errorMessage = lce.cipher.decrypt(self.privateKey, answer)
  if answer ~= challenge then
    Log:error(format("Erro ao obter a resposta de %s: %s", name, errorMessage))
    return false, self.invalidCredential, self.invalidLease
  end
  self.challenges[name] = nil
  local entry = self:addEntry(name, true)
  return true, entry.credential, entry.lease.duration
end

---
--Obt�m o desafio para um membro.
--
--@param name O nome do membro.
--
--@return O desafio.
--
--@see loginByCertificate
---
function ACSFacet:getChallenge(name)
  local cert, err = self.certificateDB:get(name)
  if cert then
    cert = lce.x509.readfromderstring(cert)
    return self:generateChallenge(name, cert)
  end
  Log:error(format("Falha ao recuperar certificado de '%s': %s", name, err))
  return ""
end

---
--Gera um desafio para um membro.
--
--@param name O nome do membro.
--@param certificate O certificado do membro.
--
--@return O desafio.
---
function ACSFacet:generateChallenge(name, certificate)
  local randomSequence = tostring(luuid.new("time"))
  self.challenges[name] = randomSequence
  return lce.cipher.encrypt(certificate:getpublickey(), randomSequence)
end

---
--Faz o logout de uma credencial.
--
--@param credential A credencial.
--
--@return true caso a credencial estivesse logada, ou false caso contr�rio.
---
function ACSFacet:logout(credential)
  local entry = self.entries[credential.identifier]
  if not entry then
    Log:warn("Tentativa de logout com credencial inexistente: "..
      credential.identifier)
    return false
  end
  self:removeEntry(entry)
  -- removendo conex�o com o servi�o de registro.
  local success, conns =
        oil.pcall(self.context.IReceptacles.getConnections,
                  self.context.IReceptacles, "RegistryServiceReceptacle")
  if not success then
    Log:warn("Erro remover conex�o com servi�o de registro.")
    Log:warn(conns)
  else
    for _, desc in pairs(conns) do
      self.context.IReceptacles:disconnect(desc.id)
    end
  end
  return true
end

---
--Verifica se uma credencial � v�lida.
--
--@param credential A credencial.
--
--@return true caso a credencial seja v�lida, ou false caso contr�rio.
---
function ACSFacet:isValid(credential)
  local entry = self.entries[credential.identifier]
  if not entry then
    return false
  end
  if entry.credential.delegate ~= "" and not entry.certified then
    return false
  end
  return true
end

---
--Verifica se uma credencial � v�lida e retorna sua entrada completa.
--
--@param credential A credencial.
--
--@return a credencial caso exista, ou nil caso contr�rio.
---
function ACSFacet:getEntryCredential(credential)
  local emptyEntry = {     credential = {  identifier = "",  owner = "",  delegate = "" },
			    certified = false,
			    lease = 0,
			    observers = {},
			    observedBy = ""
			}
  local entry = self.entries[credential.identifier]

  if not entry or entry == nil then
    return emptyEntry
  end
  if entry.credential.delegate ~= "" and not entry.certified then
    return emptyEntry
  end
  return entry
end

---
--Adiciona um observador de credenciais.
--
--@param observer O observador.
--@param credentialIdentifiers As credenciais de interesse do observador.
--
--@return O identificador do observador.
---
function ACSFacet:addObserver(observer, credentialIdentifiers)
  local observerId = self:generateObserverIdentifier()

  local observerEntry = {observer = observer, credentials = {}}
  self.observers[observerId] = observerEntry

  local credential = Openbus:getInterceptedCredential()
  self.entries[credential.identifier].observers[observerId] = true

  for _, credentialId in ipairs(credentialIdentifiers) do
    local entry = self.entries[credentialId]
    if entry then
      entry.observedBy[observerId] = true
      observerEntry.credentials[credentialId] = true
    end
  end

  return observerId
end

---
--Adiciona uma credencial � lista de credenciais de um observador.
--
--@param observerIdentifier O identificador do observador.
--@param credentialIdentifier O identificador da credencial.
--
--@return true caso a credencil tenha sido adicionada, ou false caso contr�rio.
---
function ACSFacet:addCredentialToObserver(observerIdentifier, credentialIdentifier)
  local entry = self.entries[credentialIdentifier]
  if not entry then
    return false
  end

  local observerEntry = self.observers[observerIdentifier]
  if not observerEntry then
    return false
  end

  entry.observedBy[observerIdentifier] = true
  observerEntry.credentials[credentialIdentifier] = true

  return true
end

---
--Remove um observador e retira sua credencial da lista de outros observadores.
--
--@param observerIdentifier O identificador do observador.
--@param credential A credencial.
--
--@return true caso o observador tenha sido removido, ou false caso contr�rio.
---
function ACSFacet:removeObserver(observerIdentifier, credential)
  local observerEntry = self.observers[observerIdentifier]
  if not observerEntry then
    return false
  end
  for credentialId in pairs(observerEntry.credentials) do
    self.entries[credentialId].observedBy[observerIdentifier] = nil
  end
  self.observers[observerIdentifier] = nil
  credential = credential or Openbus:getInterceptedCredential()
  self.entries[credential.identifier].observers[observerIdentifier] = nil
  return true
end

---
--Remove uma credencial da lista de credenciais de um observador.
--
--@param observerIdentifier O identificador do observador.
--@param credentialIdentifier O identificador da credencial.
--
--@return true caso a credencial seja removida, ou false caso contr�rio.
---
function ACSFacet:removeCredentialFromObserver(observerIdentifier,
    credentialIdentifier)
  local observerEntry = self.observers[observerIdentifier]
  if not observerEntry then
    return false
  end
  observerEntry.credentials[credentialIdentifier] = nil
  local entry = self.entries[credentialIdentifier]
  if not entry then
    return false
  end
  entry.observedBy[observerIdentifier] = nil
  return true
end

---
--Adiciona uma credencial ao banco de dados.
--
--@param name O nome da entidade para a qual a credencial ser� gerada.
--
--@return A credencial.
---
function ACSFacet:addEntry(name, certified)
  local credential = {
    identifier = self:generateCredentialIdentifier(),
    owner = name,
    delegate = "",
  }
  local duration = self.deltaT
  local lease = { lastUpdate = os.time(), duration = duration }
  local entry = {
    credential = credential,
    certified = certified,
    lease = lease,
    observers = {},
    observedBy = {}
  }
  self.credentialDB:insert(entry)
  self.entries[entry.credential.identifier] = entry
  return entry
end

---
--Gera um identificador de credenciais.
--
--@return O identificador de credenciais.
---
function ACSFacet:generateCredentialIdentifier()
  return luuid.new("time")
end

---
--Gera um identificador de observadores de credenciais.
--
--@return O identificador de observadores de credenciais.
---
function ACSFacet:generateObserverIdentifier()
  return luuid.new("time")
end

---
--Remove uma credencial da base de dados e notifica os observadores sobre tal
--evento.
--
--@param entry A credencial.
---
function ACSFacet:removeEntry(entry)
  local credential = entry.credential
  self:notifyCredentialWasDeleted(credential)
  for observerId in pairs(self.entries[credential.identifier].observers) do
    self:removeObserver(observerId, credential)
  end
  for observerId in pairs(self.entries[credential.identifier].observedBy) do
    self:removeCredentialFromObserver(observerId, credential.identifier)
  end
  self.entries[credential.identifier] = nil
  self.credentialDB:delete(entry)
end

--
-- Invalida uma credential de um dado membro.
--
-- @param name OI do membro.
--
function ACSFacet:removeEntryById(name)
  local found
  for _, entry in pairs(self.entries) do
    if entry.credential.owner == name then
      found = entry
      break
    end
  end
  if found then
    self:removeEntry(found)
  end
end

---
--Envia aos observadores a notifica��o de que um credencial n�o existe mais.
--
--@param credential A credencial.
---
function ACSFacet:notifyCredentialWasDeleted(credential)
  for observerId in pairs(self.entries[credential.identifier].observedBy) do
    local observerEntry = self.observers[observerId]
    if observerEntry then
      local success, err =
        oil.pcall(observerEntry.observer.credentialWasDeleted,
                  observerEntry.observer, credential)
      if not success then
        Log:warn("Erro ao notificar um observador.")
        Log:warn(err)
      end
    end
  end
end

--------------------------------------------------------------------------------
-- Faceta ILeaseProvider
--------------------------------------------------------------------------------

LeaseProviderFacet = oop.class{}

---
--@see openbus.common.LeaseProvider#renewLease
---
function LeaseProviderFacet:renewLease(credential)
  self = self.context.IAccessControlService
  Log:lease(credential.owner.. " renovando lease.")
  if not self:isValid(credential) then
    Log:warn(credential.owner.. " credencial inv�lida.")
    return false, self.invalidLease
  end
  local now = os.time()
  local lease = self.entries[credential.identifier].lease
  lease.lastUpdate = now
  lease.secondChance = false
  -- Por enquanto deixa o lease com tempo fixo
  return true, self.deltaT
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
-- Carrega os objetos das bases de dados.
--
function ManagementFacet:loadData()
  -- Cache de objetos
  self.systems = {}
  self.deployments = {}
  -- Carrega os sistemas
  local data = assert(self.systemDB:getValues())
  for _, info in ipairs(data) do
    self.systems[info.id] = true
  end
  -- Carrega os dados e cria as implanta��es dos sistemas
  data = assert(self.deploymentDB:getValues())
  for _, info in ipairs(data) do
    self.deployments[info.id] = true
  end
end

---
-- Cadastra um novo sistema.
--
-- @param id Identificador �nico do sistema.
-- @param description Descri��o do sistema.
-- 
function ManagementFacet:addSystem(id, description)
  self:checkPermission()
  if self.systems[id] then
    Log:error(format("Sistema '%s' j� cadastrado.", id))
    error{"IDL:openbusidl/acs/SystemAlreadyExists:1.0"}
  end
  local succ, msg = self.systemDB:save(id, {
    id = id,
    description = description
  })
  if not succ then
    Log:error(format("Falha ao salvar sistema '%s': %s", id, msg))
  end
  self.systems[id] = true
end

---
-- Remove o sistema do barramento. Um sistema s� poder� ser removido
-- se n�o possuir nenhuma implanta��o cadastrada que o referencia.
--
-- @param id Identificador do sistema.
--
function ManagementFacet:removeSystem(id)
  self:checkPermission()
  if not self.systems[id] then
    Log:error(format("Sistema '%s' n�o cadastrado.", id))
    error{"IDL:openbusidl/acs/SystemNonExistent:1.0"}
  end
  local depls = self.deploymentDB:getValues()
  for _, depl in ipairs(depls) do
    if depl.systemId == id then
      Log:error(format("Sistema '%s' em uso.", id))
      error{"IDL:openbusidl/acs/SystemInUse:1.0"}
    end
  end
  self.systems[id] = nil
  local succ, msg = self.systemDB:remove(id)
  if not succ then
    Log:error(format("Falha ao remover sistema '%s': %s", id, msg))
  end
end

---
-- Atualiza a descri��o do sistema.
-- 
-- @param id Identificador do sistema.
-- @param description Nova descri��o para o sistema.
-- 
function ManagementFacet:setSystemDescription(id, description)
  self:checkPermission()
  if not self.systems[id] then
    Log:error(format("Sistema '%s' n�o cadastrado.", id))
    error{"IDL:openbusidl/acs/SystemNonExistent:1.0"}
  end
  local system, msg = self.systemDB:get(id)
  if system then
    local succ
    system.description = description
    succ, msg = self.systemDB:save(id, system)
    if not succ then
      Log:error(format("Falha ao salvar sistema '%s': %s", id, msg))
    end
  else
    Log:error(format("Falha ao recuperar sistema '%s': %s", id, msg))
  end
end

---
-- Recupera todos os sistemas cadastrados.
-- 
-- @return Uma seq��ncia de sistemas.
-- 
function ManagementFacet:getSystems()
  local systems, msg = self.systemDB:getValues()
  if not systems then
    Log:error(format("Falha ao recuperar os sistemas: %s", msg))
  end
  return systems
end

--- 
-- Recupera um sistema dado o seu identificador.
--
-- @param id Identificador do sistema.
--
-- @return Sistema referente ao identificador.
--
function ManagementFacet:getSystemById(id)
  if not self.systems[id] then
    Log:error(format("Sistema '%s' n�o cadastrado.", id))
    error{"IDL:openbusidl/acs/SystemNonExistent:1.0"}
  end
  local system, msg = self.systemDB:get(id)
  if not system then
    Log:error(format("Falha ao recuperar os sistemas: %s", msg))
  end
  return system
end

-------------------------------------------------------------------------------

---
-- Cadastra uma nova implanta��o para um sistema.
--
-- @param id Identificador �nico da implanta��o (estilo login UNIX).
-- @param systeId Identificador do sistema a que esta implanta��o pertence.
-- @param description Descri��o da implanta��o.
--
function ManagementFacet:addSystemDeployment(id, systemId, description, 
                                             certificate)
  self:checkPermission()
  if self.deployments[id] then
    Log:error(format("Implanta��o '%s' j� cadastrada.", id))
    error{"IDL:openbusidl/acs/SystemDeploymentAlreadyExists:1.0"}
  end
  if not self.systems[systemId] then
    Log:error(format("Falha ao criar implanta��o '%s': sistema %s "..
                     "n�o cadastrado.", id, systemId))
    error{"IDL:openbusidl/acs/SystemNonExistent:1.0"}
  end
  local succ, msg = lce.x509.readfromderstring(certificate)
  if not succ then
    Log:error(format("Falha ao criar implanta��o '%s': certificado inv�lido.",
      id))
    error{"IDL:openbusidl/acs/InvalidCertificate:1.0"}
  end
  self.deployments[id] = true
  succ, msg = self.deploymentDB:save(id, {
    id = id,
    systemId = systemId,
    description = description,
  })
  if not succ then
    Log:error(format("Falha ao salvar implanta��o %s na base de dados: %s",
      id, msg))
  end
  succ, msg = self.certificateDB:save(id, certificate)
  if not succ then
    Log:error(format("Falha ao salvar certificado de '%s': %s", id, msg))
  end
end

---
-- Remove uma implanta��o de sistema.
--
-- @param id Identificador da implanta��o.
--
function ManagementFacet:removeSystemDeployment(id)
  self:checkPermission()
  if not self.deployments[id] then
    Log:error(format("Implanta��o '%s' n�o cadastrada.", id))
    error{"IDL:openbusidl/acs/SystemDeploymentNonExistent:1.0"}
  end
  self.deployments[id] = nil
  local succ, msg = self.deploymentDB:remove(id)
  if not succ then
    Log:error(format("Falha ao remover implanta��o '%s' da base de dados: %s",
      id, msg))
  end
  succ, msg = self.certificateDB:remove(id)
  if not succ and msg ~= "not found" then
    Log:error(format("Falha ao remover certificado da implanta��o '%s': %s",
      id, msg))
  end
  -- Invalida a credencial do membro que est� sendo removido
  local acs = self.context.IAccessControlService
  acs:removeEntryById(id)
  -- Remove todas as autoriza��es da implanta��o
  local rs = acs:getRegistryService()
  if rs then
    local orb = Openbus:getORB()
    local ic = rs:_component()
    ic = orb:narrow(ic, "IDL:scs/core/IComponent:1.0")
    rs = ic:getFacetByName("IManagement")
    rs = orb:narrow(rs, "IDL:openbusidl/rs/IManagement:1.0")
    rs.__try:removeAuthorization(id)
  end
end

---
-- Altera a descri��o da implanta��o.
--
-- @param id Identificador da implanta��o.
-- @param description Nova descri��o da implanta��o.
--
function ManagementFacet:setSystemDeploymentDescription(id, description)
  self:checkPermission()
  if not self.deployments[id] then
    Log:error(format("Implanta��o '%s' n�o cadastrada.", id))
    error{"IDL:openbusidl/acs/SystemDeploymentNonExistent:1.0"}
  end
  local depl, msg = self.deploymentDB:get(id)
  if not depl then
    Log:error(format("Falha ao recuperar implanta��o '%s': %s", id, msg))
  else
    local succ
    depl.description = description
    succ, msg = self.deploymentDB:save(id, depl)
    if not succ then
      Log:error(format("Falha ao salvar implanta��o '%s' na base de dados: %s",
        id, msg))
    end
  end
end

---
-- Recupera o certificado da implanta��o.
-- 
-- @param id Identificador da implanta��o.
-- 
-- @return Certificado da implanta��o.
--
function ManagementFacet:getSystemDeploymentCertificate(id)
  if not self.deployments[id] then
    Log:error(format("Implanta��o '%s' n�o cadastrada.", id))
    error{"IDL:openbusidl/acs/SystemDeploymentNonExistent:1.0"}
  end
  local cert, msg = self.certificateDB:get(id)
  if cert then
     return cert
  elseif msg == "not found" then
     Log:error(format("Implanta��o '%s' n�o possui certificado.", id))
     error{"IDL:openbusidl/acs/CertificateNonExistent:1.0"}
  else
    Log:error(format("Falha ao recuperar certificado de '%s': %s", id, msg))
  end
end

---
-- Altera o certificado da implanta��o.
--
-- @param id Identificador da implanta��o.
-- @param certificate Novo certificado da implanta��o.
--
function ManagementFacet:setSystemDeploymentCertificate(id, certificate)
  self:checkPermission()
  if not self.deployments[id] then
    Log:error(format("Implanta��o '%s' n�o cadastrada.", id))
    error{"IDL:openbusidl/acs/SystemDeploymentNonExistent:1.0"}
  end
  local tmp, msg = lce.x509.readfromderstring(certificate)
  if not tmp then
    Log:error(format("%s: certificado inv�lido.", id, msg))
    error{"IDL:openbusidl/acs/InvalidCertificate:1.0"}
  end
  local succ, msg = self.certificateDB:save(id, certificate)
  if not succ then
    Log:error(format("Falha ao salvar certificado de '%s': %s", id, msg))
  end
end

---
-- Recupera todas implanta��es cadastradas.
--
-- @return Uma seq��ncia com as implanta��es cadastradas. 
--
function ManagementFacet:getSystemDeployments()
  local depls, msg = self.deploymentDB:getValues()
  if not depls then
    Log:error(format("Falha ao recuperar implanta��es: %s", msg))
  end
  return depls
end

---
-- Recupera a implanta��o dado o seu identificador.
--
-- @return Retorna a implanta��o referente ao identificador.
--
function ManagementFacet:getSystemDeployment(id)
  if not self.deployments[id] then
    Log:error(format("Implanta��o '%s' n�o cadastrada.", id))
    error{"IDL:openbusidl/acs/SystemDeploymentNonExistent:1.0"}
  end
  local depl, msg = self.deploymentDB:get(id)
  if not depl then
    Log:error(format("Falha ao recuperar implanta��o '%s': %s", id, msg))
  end
  return depl
end

---
-- Recupera todas as implanta��es de um dado sistema.
--
-- @param systemId Identificador do sistema 
--
-- @return Seq��ncia com as implanta��es referentes ao sistema informado.
--
function ManagementFacet:getSystemDeploymentsBySystemId(systemId)
  local array = {}
  local depls, msg = self.deploymentDB:getValues()
  if not depls then
    Log:error(format("Falha ao recuperar implanta��es: %s", msg))
  else
    for _, depl in pairs(depls) do
      if depl.systemId == systemId then
        array[#array+1] = depl
      end
    end
  end
  return array
end

--------------------------------------------------------------------------------
-- Faceta IComponent
--------------------------------------------------------------------------------

---
--Inicia o componente.
--
--@see scs.core.IComponent#startup
---
function startup(self)
  local path
  local mgm = self.context.IManagement
  local acs = self.context.IAccessControlService
  local config = acs.config

  -- O ACS precisa configurar os interceptadores manualmente 
  -- pois n�o realiza conex�o.
  Openbus.acs = acs
  Openbus:_setInterceptors()
  
  -- Administradores dos servi�os
  mgm.admins = {}
  for _, name in ipairs(config.administrators) do
     mgm.admins[name] = true
  end

  -- Inicializa as base de dados de gerenciamento
  mgm.systemDB = TableDB(DATA_DIR .. "/acs_system.db")
  mgm.deploymentDB = TableDB(DATA_DIR .. "/acs_deployment.db")
  -- Carrega a cache
  mgm:loadData()

  -- Inicializa a ger�ncia de certificados
  if string.match(config.certificatesDirectory, "^/") then
    path = config.certificatesDirectory
  else
    path = DATA_DIR .. "/" .. config.certificatesDirectory
  end
  acs.certificateDB = CertificateDB(path)
  mgm.certificateDB = acs.certificateDB

  -- Carrega chave privada
  if string.match(config.privateKeyFile, "^/") then
    path = config.privateKeyFile
  else
    path = DATA_DIR .. "/" .. config.privateKeyFile
  end
  acs.privateKey = lce.key.readprivatefrompemfile(path)

  -- Inicializa repositorio de credenciais
  local acsEntry
  if string.match(config.databaseDirectory, "^/") then
    path = config.databaseDirectory
  else
    path = DATA_DIR .. "/" .. config.databaseDirectory
  end
  acs.credentialDB = CredentialDB(path)
  local entriesDB = acs.credentialDB:retrieveAll()
  for _, entry in pairs(entriesDB) do
    entry.lease.lastUpdate = os.time()
    acs.entries[entry.credential.identifier] = entry -- Deveria fazer c�pia?
    if entry.credential.owner == "AccessControlService" then
      acsEntry = entry
    elseif entry.component and entry.credential.owner == "RegistryService" then
      acs.registryService = {
        credential = entry.credential,
        component = entry.component,
      }
    end
  end

  -- Se a credencial do ACS n�o existir (primeira execu��o), criar uma nova
  acsEntry = acsEntry or acs:addEntry("AccessControlService", true)
  -- Credencial n�o expira
  acsEntry.lease.duration = math.huge
  Openbus:setCredential(acsEntry.credential)

  -- Controle de leasing
  acs.checkExpiredLeases = function()
    -- Uma corotina s� percorre a tabela de tempos em tempos
    -- ou precisamos acordar na hora "exata" que cada lease expira
    -- pra verificar?
    for id, entry in pairs(acs.entries) do
      Log:lease("Verificando a credencial de "..id)
      local credential = entry.credential
      local lastUpdate = entry.lease.lastUpdate
      local secondChance = entry.lease.secondChance
      local duration = entry.lease.duration
      local now = os.time()
      if (os.difftime (now, lastUpdate) > duration ) then
        if secondChance then
          Log:warn(credential.owner.. " lease expirado: LOGOUT.")
          acs:logout(credential) -- you may clear existing fields.
        else
          entry.lease.secondChance = true
        end
      end
    end
  end
  acs.leaseProvider = LeaseProvider(acs.checkExpiredLeases, acs.deltaT)
  --self = self.context.IFaultTolerantService
  self.context.IFaultTolerantService:setStatus(true)
end

---
--Finaliza o servi�o.
--
--@see scs.core.IComponent#shutdown
---
function shutdown(self)
  Log:service("Pedido de shutdown para servi�o de controle de acesso")
  local acs = self.context.IAccessControlService
  acs.leaseProvider:stopCheck()
  local orb = Openbus:getORB()
  orb:deactivate(acs)
  orb:deactivate(self.context.IManagement)
  orb:shutdown()
  Log:faulttolerance("Servico de Controle de Acesso matou seu processo.")
end
