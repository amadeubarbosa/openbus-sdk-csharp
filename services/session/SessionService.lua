-- $Id$

local oil = require "oil"
local luuid = require "uuid"

local Session = require "openbus.services.session.Session"

local Log = require "openbus.common.Log"

local oop = require "loop.base"

---
--Faceta que disponibiliza a funcionalidade b�sica do servi�o de sess�o.
---
module("openbus.services.session.SessionService", oop.class)

---
--Cria a facete de um Servi�o de Sess�o.
--
--@param accessControlService O Servi�o de Controle de Acesso.
--@param serverInterceptor O interceptador de servidor que ser� instalado para
--este servi�o.
--
--@return A faceta do Servi�o de Sess�o.
---
function __init(self, accessControlService, serverInterceptor)
  return oop.rawnew(self, {
    sessions = {},
    serverInterceptor = serverInterceptor,
    accessControlService = accessControlService,
  })
end

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
function createSession(self, member)
  local credential = self.serverInterceptor:getCredential()
  if self.sessions[credential.identifier] then
    Log:err("Tentativa de criar sess�o j� existente")
    return false
  end
  Log:service("Vou criar sess�o")
  local session = Session(self:generateIdentifier(), credential)
  session = oil.newservant(session, "IDL:openbusidl/ss/ISession:1.0")
  self.sessions[credential.identifier] = session
  Log:service("Sess�o criada!")

  -- A credencial deve ser observada!
  if not self.observerId then
    local observer = {
      sessionService = self,
      credentialWasDeleted =
        function(self, credential)
          self.sessionService:credentialWasDeleted(credential)
        end
    }
    self.observer = oil.newservant(observer,
                                  "IDL:openbusidl/acs/ICredentialObserver:1.0",
                                  "SessionServiceCredentialObserver")
    self.observerId =
      self.accessControlService:addObserver(self.observer,
                                            {credential.identifier})
  else
    self.accessControlService:addCredentialToObserver(self.observerId,
                                                     credential.identifier)
  end

  -- Adiciona o membro � sess�o
  local memberID = session:addMember(member)
  return true, session, memberID
end

---
--Notifica��o de dele��o de credencial (logout).
--
--@param credential A credencial removida.
---
function credentialWasDeleted(self, credential)

  -- Remove a sess�o
  local session = self.sessions[credential.identifier]
  if session then
  Log:service("Removendo sess�o de credencial deletada ("..
              credential.identifier..")")
    session:_deactivate()
    self.sessions[credential.identifier] = nil
  end
end

---
--Gera um identificador de sess�o.
--
--@return Um identificador de sess�o.
---
function generateIdentifier()
  return luuid.new("time")
end

---
--Obt�m a sess�o associada a uma credencial. A credencial em quest�o �
--recuperada da requisi��o pelo interceptador do servi�o, e repassada atrav�s
--do objeto PICurrent.
--
--@return A sess�o, ou nil, caso n�o exista sess�o para a credencial do membro.
---
function getSession(self)
  local credential = self.serverInterceptor:getCredential()
  local session = self.sessions[credential.identifier]
  if not session then
   Log:warn("N�o h� sess�o para "..credential.identifier)
    return nil
  end
  return session
end

---
--Procedimento ap�s a reconex�o do servi�o.
---
function wasReconnected(self)

  -- registra novamente o observador de credenciais
  self.observerId = self.accessControlService:addObserver(self.observer, {})
  Log:service("Observador recadastrado")

  -- Mant�m apenas as sess�es com credenciais v�lidas
  local invalidCredentials = {}
  for credentialId, session in pairs(self.sessions) do
    if not self.accessService.addCredentialToObserver(self.observerId,
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

---
--Finaliza o servi�o.
---
function shutdown(self)
  if self.observerId then
    self.accessControlService:removeObserver(self.observerId)
    self.observer:_deactivate()
    self.observer = nil
    self.observerId = nil
  end
end
