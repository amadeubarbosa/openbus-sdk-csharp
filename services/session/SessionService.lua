-- $Id$

local oil = require "oil"
local Openbus = require "openbus.Openbus"
local orb = oil.orb

local luuid = require "uuid"

local Session = require "core.services.session.Session"

local Log = require "openbus.util.Log"

local oop = require "loop.base"

local scs = require "scs.core.base"

local tostring = tostring
local table    = table
local pairs    = pairs
local ipairs   = ipairs

---
--Faceta que disponibiliza a funcionalidade b�sica do servi�o de sess�o.
---
module "core.services.session.SessionService"

--------------------------------------------------------------------------------
-- Faceta ISessionService
--------------------------------------------------------------------------------

SessionService = oop.class{sessions = {}, invalidMemberIdentifier = ""}

-----------------------------------------------------------------------------
-- Descricoes do Componente Sessao
-----------------------------------------------------------------------------

-- Facet Descriptions
local facetDescriptions = {}
facetDescriptions.IComponent       = {}
facetDescriptions.IMetaInterface   = {}
facetDescriptions.SessionEventSink = {}
facetDescriptions.ISession         = {}

facetDescriptions.IComponent.name                 = "IComponent"
facetDescriptions.IComponent.interface_name       = "IDL:scs/core/IComponent:1.0"
facetDescriptions.IComponent.class                = scs.Component

facetDescriptions.IMetaInterface.name             = "IMetaInterface"
facetDescriptions.IMetaInterface.interface_name   = "IDL:scs/core/IMetaInterface:1.0"
facetDescriptions.IMetaInterface.class            = scs.MetaInterface

facetDescriptions.SessionEventSink.name           = "SessionEventSink"
facetDescriptions.SessionEventSink.interface_name = "IDL:openbusidl/ss/SessionEventSink:1.0"
facetDescriptions.SessionEventSink.class          = Session.SessionEventSink

facetDescriptions.ISession.name                   = "ISession"
facetDescriptions.ISession.interface_name         = "IDL:openbusidl/ss/ISession:1.0"
facetDescriptions.ISession.class                  = Session.Session

-- Receptacle Descriptions
local receptacleDescriptions = {}

-- component id
local componentId = {}
componentId.name = "Session"
componentId.major_version = 1
componentId.minor_version = 0
componentId.patch_version = 0
componentId.platform_spec = ""

---
--Cria uma sess�o associada a uma credencial. A credencial em quest�o �
--recuperada da requisi��o pelo interceptador do servi�o, e repassada atrav�s
--do objeto PICurrent.
--
--@param member O membro que est� solicitando a cria��o da sess�o e que estar�
--inserido na sess�o automaticamente.
--
--@return true, a sess�o e o identificador de membro da sess�o em caso de
--sucesso, ou false, caso contr�rio.
---
function SessionService:createSession(member)
  local credential = Openbus:getInterceptedCredential()
  if self.sessions[credential.identifier] then
    Log:err("Tentativa de criar sess�o j� existente")
    return false, nil, self.invalidMemberIdentifier
  end
  Log:service("Criando sess�o")
  local session = scs.newComponent(facetDescriptions, receptacleDescriptions, componentId)
  session.ISession.identifier = self:generateIdentifier()
  session.ISession.credential = credential
  self.sessions[credential.identifier] = session
  Log:service("Sessao criada com id "..tostring(session.ISession.identifier).." !")

  -- A credencial deve ser observada!
  if not self.observerId then
    self.observerId =
      self.accessControlService:addObserver(self.context.ICredentialObserver,
                                            {credential.identifier})
  else
    self.accessControlService:addCredentialToObserver(self.observerId,
                                                     credential.identifier)
  end

  -- Adiciona o membro � sess�o
  local memberID = session.ISession:addMember(member)
  return true, session.IComponent, memberID
end

---
--Notifica��o de dele��o de credencial (logout).
--
--@param credential A credencial removida.
---
function SessionService:credentialWasDeleted(credential)

  -- Remove a sess�o
  local session = self.sessions[credential.identifier]
  if session then
    Log:service("Removendo sess�o de credencial deletada ("..
      credential.identifier..")")
    orb:deactivate(session.ISession)
    orb:deactivate(session.IMetaInterface)
    orb:deactivate(session.SessionEventSink)
    orb:deactivate(session.IComponent)

    self.sessions[credential.identifier] = nil
  end
end

---
--Gera um identificador de sess�o.
--
--@return Um identificador de sess�o.
---
function SessionService:generateIdentifier()
  return luuid.new("time")
end

---
--Obt�m a sess�o associada a uma credencial. A credencial em quest�o �
--recuperada da requisi��o pelo interceptador do servi�o, e repassada atrav�s
--do objeto PICurrent.
--
--@return A sess�o, ou nil, caso n�o exista sess�o para a credencial do membro.
---
function SessionService:getSession()
  local credential = Openbus:getInterceptedCredential()
  local session = self.sessions[credential.identifier]
  if not session then
   Log:warn("N�o h� sess�o para "..credential.identifier)
    return nil
  end
  return session.IComponent
end

---
--Procedimento ap�s a reconex�o do servi�o.
---
function SessionService:wasReconnected()

  -- registra novamente o observador de credenciais
  self.observerId = self.accessControlService:addObserver(self.context.ICredentialObserver, {})
  Log:service("Observador recadastrado")

  -- Mant�m apenas as sess�es com credenciais v�lidas
  local invalidCredentials = {}
  for credentialId, session in pairs(self.sessions) do
    if not self.accessService:addCredentialToObserver(self.observerId,
        credentialId) then
      Log:service("Sess�o para "..credentialId.." ser� removida")
      table.insert(invalidCredentials, credentialId)
    else
      Log:service("Sess�o para "..credentialId.." ser� mantida")
    end
  end
  for _, credentialId in ipairs(invalidCredentials) do
    self.sessions[credentialId] = nil
  end
end

--------------------------------------------------------------------------------
-- Faceta ICredentialObserver
--------------------------------------------------------------------------------

Observer = oop.class{}

function Observer:credentialWasDeleted(credential)
  self.context.ISessionService:credentialWasDeleted(credential)
end

