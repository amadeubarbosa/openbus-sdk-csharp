-- $Id$

local tostring = tostring
local ipairs = ipairs
local pairs = pairs

local oil = require "oil"
local orb = oil.orb

local luuid = require "uuid"

local Log = require "openbus.common.Log"

local oop = require "loop.base"

---
--Sess�o compartilhada pelos membros associados a uma mesma credencial.
---
module "core.services.session.Session"

--------------------------------------------------------------------------------
-- Faceta ISession
--------------------------------------------------------------------------------

Session = oop.class{sessionMembers = {}}

---
--Obt�m o identificador da sess�o.
--
--@return O identificador da sess�o.
---
function Session:getIdentifier()
  return self.identifier
end

---
--Adiciona um membro a sess�o.
--
--@param member O membro a ser adicionado.
--
--@return O identificador do membro na sess�o.
---
function Session:addMember(member)
  local memberName = member:getComponentId().name
  Log:service("Membro "..memberName.." adicionado a sess�o")
  local identifier = self:generateMemberIdentifier()
  self.sessionMembers[identifier] = member

  -- verifica se o membro recebe eventos
  local eventSinkInterface = "IDL:openbusidl/ss/SessionEventSink:1.0"
  local eventSink = member:getFacet(eventSinkInterface)
  if eventSink then
    Log:service("Membro "..memberName.." receber� eventos")
    local eventSinks = self.context.SessionEventSink.eventSinks
    eventSinks[identifier] =  orb:narrow(eventSink,
        eventSinkInterface)
  else
    Log:service("Membro "..memberName.." n�o receber� eventos")
  end
  return identifier
end

---
--Remove um membro da sess�o.
--
--@param identifier O identificador do membro na sess�o.
--
--@return true caso o membro tenha sido removido da sess�o, ou false caso
--contr�rio.
---
function Session:removeMember(identifier)
  member = self.sessionMembers[identifier]
  if not member then
    return false
  end
  Log:service("Membro "..member:getComponentId().name.." removido da sess�o")
  self.sessionMembers[identifier] = nil
  self.context.SessionEventSink.eventSinks[identifier] = nil
  return true
end

---
--Obt�m a lista de membros de uma sess�o.
--
--@return Os membros da sess�o.
---
function Session:getMembers()
  local members = {}
  for _, member in pairs(self.sessionMembers) do
    table.insert(members, member)
  end
  return members
end

---
--Gera um identificador de membros de sess�o.
--
--@return O identificador de membro de sess�o.
---
function Session:generateMemberIdentifier()
  return luuid.new("time")
end

--------------------------------------------------------------------------------
-- Faceta SessionEventSink
--------------------------------------------------------------------------------

SessionEventSink = oop.class{eventSinks = {}}

---
--Repassa evento para membros da sess�o.
--
--@param event O evento.
---
function SessionEventSink:push(event)
  Log:service("Repassando evento "..event.type.." para membros de sess�o")
  for _, sink in pairs(self.eventSinks) do
    local result, errorMsg = oil.pcall(sink.push, sink, event)
    if not result then
      Log:service("Erro ao enviar evento para membro de sess�o: "..errorMsg)
    end
  end
end

---
--Solicita a desconex�o de todos os membros da sess�o.
---
function SessionEventSink:disconnect(self)
  Log:service("Desconectando os membros da sess�o")
  for _, sink in pairs(self.eventSinks) do
    sink:disconnect()
  end
end


