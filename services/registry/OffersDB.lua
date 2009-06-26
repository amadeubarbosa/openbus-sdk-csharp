-- $Id$
local io = io
local string = string
local os = os

local error = error

local lfs = require "lfs"
local oil = require "oil"
local orb = oil.orb

local FileStream = require "loop.serial.FileStream"

local Log = require "openbus.util.Log"

local oop = require "loop.base"

---
--Mecanismo de persist�ncia de ofertas de servi�o.
--
module("core.services.registry.OffersDB", oop.class)

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
  local mode = lfs.attributes(databaseDirectory, "mode")
  if not mode then
    Log:service("O diretorio ["..databaseDirectory.."] nao foi encontrado. "..
        "Criando...")
    local status, errorMessage = lfs.mkdir(databaseDirectory)
    if not status then
      Log:error("Nao foi possivel criar o diretorio ["..databaseDirectory.."]: "..errorMessage)
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
  local offerFiles = lfs.dir(self.databaseDirectory)
  local offerEntries = {}
  for fileName in offerFiles do
    if string.sub(fileName, -(#self.FILE_SUFFIX)) == self.FILE_SUFFIX then
      local offerFile = io.open(self.databaseDirectory..self.FILE_SEPARATOR..fileName)
      local stream = FileStream{
        file = offerFile
      }
      local offerEntry = stream:get()
      offerFile:close()

      self.dbOffers[offerEntry.identifier] = true

      -- caso especial para referencias a membros
      local memberIOR = offerEntry.offer.member
      offerEntry.offer.member = orb:newproxy(memberIOR)

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
  local stream = FileStream{
    file = offerFile,
    getmetatable = false,
  }

  local member =  offerEntry.offer.member
  stream[member] = "'"..orb:tostring(member).."'"
  stream:put(offerEntry)
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
