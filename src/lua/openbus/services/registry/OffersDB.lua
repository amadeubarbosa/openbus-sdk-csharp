-- $Id$

local io = io
local string = string
local os = os

local ipairs = ipairs
local pairs = pairs
local dofile = dofile
local type = type
local tostring = tostring
local tonumber = tonumber

local lposix = require "lposix"
local oil = require "oil"

local Log = require "openbus.common.Log"

local oop = require "loop.base"

---
--Mecanismo de persist�ncia de ofertas de servi�o.
--
module("openbus.services.registry.OffersDB", oop.class)

FILE_SUFFIX = ".offer"
FILE_SEPARATOR = "/"

---
--Cria um banco de dados de ofertas de servi�o.
--
--@param databaseDirectory O diret�rio que sera utilizado para armazenas as
--ofertas de servi�o.
--
--@return O banco de dados de ofertas de servi�o.
---
function __init(self, databaseDirectory)
  if not lposix.dir(databaseDirectory) then
    Log:service("O diretorio ["..databaseDirectory.."] nao foi encontrado. "..
        "Criando...")
    local status, errorMessage = lposix.mkdir(databaseDirectory)
    if not status then
      Log:error("Nao foi possivel criar o diretorio ["..databaseDirectory.."].")
      error(errorMessage)
    end
  end
  return oop.rawnew(self, {
    databaseDirectory = databaseDirectory,
    dbOffers = {},
  })
end

---
--Obt�m todas as ofertas de servi�o.
--
--@return As ofertas de servi�o.
---
function retrieveAll(self)
  local offerFiles = lposix.dir(self.databaseDirectory)
  local offerEntries = {}
  for _, fileName in ipairs(offerFiles) do
    if string.sub(fileName, -(#self.FILE_SUFFIX)) == self.FILE_SUFFIX then
      local offerEntry = dofile(self.databaseDirectory..self.FILE_SEPARATOR..
          fileName)
      self.dbOffers[offerEntry.identifier] = true

      -- caso especial para referencias a membros
      local memberIOR = offerEntry.offer.member
      offerEntry.offer.member = oil.newproxy(memberIOR)

      offerEntries[offerEntry.identifier] = offerEntry
    end
  end
  return offerEntries
end

---
--Insere uma entrada no banco de dados.
--
--@param offerEntry A oferta de servi�o.
--
--@return true caso a oferta tenha sido inserida, ou false e uma mensagem
--de erro, caso contr�rio.
---
function insert(self, offerEntry)
  if self.dbOffers[offerEntry.identifier] then
    return false, "A oferta especificada ja existe."
  end
  local status, errorMessage = self:writeOffer(offerEntry)
  if not status then
    return false, errorMessage
  end
  self.dbOffers[offerEntry.identifier] = true
  return true
end

---
--Atualiza uma oferta de servi�o.
--
--@param offerEntry A oferta de servi�o.
--
--@return true caso a oferta seja atualizada, ou false e uma mensagem de erro,
--caso contr�rio.
---
function update(self, offerEntry)
  if not self.dbOffers[offerEntry.identifier] then
    return false, "A oferta especificada n�o existe."
  end
  return self:writeOffer(offerEntry)
end

---
--Remove uma oferta de servi�o.
--
--@param offerEntry A oferta de servi�o.
--
--@return true caso a oferta tenha sido removida, ou false e uma mensagem de
--erro, caso contr�rio.
---
function delete(self, offerEntry)
  if not self.dbOffers[offerEntry.identifier] then
    return false, "A oferta especificada n�o existe."
  end
  local status, errorMessage = self:removeOffer(offerEntry)
  if not status then
    return false, errorMessage
  end
  self.dbOffers[offerEntry.identifier] = nil
  return true
end

---
--Fun��o auxiliar que transforma um objeto em uma string.
--
--@param val O objeto.
--
--@return A string representando o objeto, ou nil caso seja um objeto de tipo
--n�o suportado.
---
function serialize(self, val)
  local t = type(val)
  if t == "table" then
    local str = '{'
    for f, s in pairs(val) do

      -- caso especial para referencias a membros (persiste o IOR)
      if type(f) == "string"  and f == "member" then
        str = str .. f .. "=[[" .. oil.tostring(s) .. "]],"
      else
        if not tonumber(f) then
          str = str .. f .. "="
        end
        str = str .. self:serialize(s) .. ","
      end
    end
    return str .. '}'
  elseif t == "string" then
    return "[[" .. val .. "]]"
  elseif t == "number" then
    return val
  elseif t == "boolean" then
    return tostring(val)
  else -- if not tab then
    return "nil"
  end
end

---
--Escreve a oferta de servi�o em arquivo.
--
--@param offerEntry A oferta de servi�o.
--
--@return true caso o arquivo seja gerado, ou false e uma mensagem de erro,
--caso contr�rio.
---
function writeOffer(self, offerEntry)
  local offerFile, errorMessage =  io.open(self.databaseDirectory..
      self.FILE_SEPARATOR..offerEntry.identifier..self.FILE_SUFFIX, "w")
  if not offerFile then
    return false, errorMessage
  end
  offerFile:write("return ".. self:serialize(offerEntry))
  offerFile:close()
  return true
end

---
--Remove o arquivo da oferta de servi�o.
--
--@param offerEntry A oferta de servi�o.
--
--@return Caso o arquivo n�o seja removido, retorna nil e uma mensagem de erro.
---
function removeOffer(self, offerEntry)
  return os.remove(self.databaseDirectory..self.FILE_SEPARATOR..
      offerEntry.identifier..self.FILE_SUFFIX)
end
