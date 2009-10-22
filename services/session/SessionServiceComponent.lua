-- $Id$

local os = os

local loadfile = loadfile
local assert = assert
local error = error
local string = string

local oil = require "oil"
local orb = oil.orb

local SessionService = require "core.services.session.SessionService"
local Openbus = require "openbus.Openbus"

local Log = require "openbus.util.Log"

local scs = require "scs.core.base"

local oop = require "loop.simple"

---
-- IComponent (membro) do Servi�o de Sess�o.
---
module "core.services.session.SessionServiceComponent"

SessionServiceComponent = oop.class({}, scs.Component)

---
--Inicia o componente.
--
--@see scs.core.IComponent#startup
---
function SessionServiceComponent:startup()
  Log:service("Pedido de startup para o servi�o de sess�o")

  local DATA_DIR = os.getenv("OPENBUS_DATADIR")

  -- Verifica se � o primeiro startup
  if not self.initialized then
    Log:service("Servi�o de sess�o est� inicializando")
    if (string.sub(self.config.privateKeyFile,1 , 1) == "/") then
      self.privateKeyFile = self.config.privateKeyFile
    else
      self.privateKeyFile = DATA_DIR.."/"..self.config.privateKeyFile
    end

    if (string.sub(self.config.accessControlServiceCertificateFile,1 , 1) == "/") then
      self.accessControlServiceCertificateFile =
        self.config.accessControlServiceCertificateFile
    else
      self.accessControlServiceCertificateFile = DATA_DIR .. "/" ..
        self.config.accessControlServiceCertificateFile
    end

    self.initialized = true
  else
    Log:service("Servi�o de sess�o j� foi inicializado")
  end

  -- autentica o servi�o, conectando-o ao barramento
  local registryService = false
  if not Openbus:isConnected() then
    registryService = Openbus:connectByCertificate(self.context._componentId.name,
      self.privateKeyFile, self.accessControlServiceCertificateFile)
    if not registryService then
      error{"IDL:SCS/StartupFailed:1.0"}
    else
      registryService = orb:narrow(registryService,
                                   "IDL:openbusidl/rs/IRegistryService:1.0")
    end
  end

  -- Cadastra callback para LeaseExpired
  Openbus:addLeaseExpiredCallback( self )

  -- obt�m a refer�ncia para o Servi�o de Controle de Acesso
  local acsFacet = Openbus:getAccessControlService()
  if not acsFacet then
    error{"IDL:SCS/StartupFailed:1.0"}
  end
  local acsIComp = acsFacet:_component()
  acsIComp = orb:narrow(acsIComp, "IDL:scs/core/IComponent:1.0")

  -- conecta-se com o controle de acesso:   [SS]--( 0--[ACS]
  local success, conId =
    oil.pcall(self.context.IReceptacles.connect, self.context.IReceptacles,
              "AccessControlServiceReceptacle", acsFacet)
  if not success then
    Log:error("Erro durante conex�o com servi�o de Controle de Acesso.")
    Log:error(conId)
    error{"IDL:SCS/StartupFailed:1.0"}
  end

  -- configura faceta ISessionService
  self.sessionService = self.context.ISessionService

  -- registra sua oferta de servi�o junto ao Servi�o de Registro
  self.serviceOffer = {
    member = self.context.IComponent,
    properties = {},
  }

  if not registryService then
    registryService = Openbus:getRegistryService()
  end
  if not registryService then
    Log:error("Servico de registro nao encontrado.\n")
    Openbus:disconnect()
    error{"IDL:SCS/StartupFailed:1.0"}
  end

  -- local success, identifier = registryService:register(self.serviceOffer)
  local success, suc, identifier =
          oil.pcall(registryService.register,registryService, self.serviceOffer)
  if not success then
    Log:error("Erro ao registrar oferta do servico de sessao.\n")
    Log:error(suc)
    Openbus:disconnect()
    error{"IDL:SCS/StartupFailed:1.0"}
  end
  self.registryIdentifier = identifier

  self.started = true
  Log:service("Servi�o de sess�o iniciado")
end

---
--Procedimento ap�s a reconex�o do servi�o.
---
function SessionServiceComponent:expired()
  Openbus:connectByCertificate(self.context._componentId.name,
      self.privateKeyFile, self.accessControlServiceCertificateFile)

  -- Procedimento realizado pela faceta
  self.sessionService:expired()

  Log:service("Servi�o de sess�o foi reconectado")

  -- Registra novamente a oferta de servi�o, pois a credencial associada
  -- agora � outra
  local registryService = Openbus:getRegistryService()
  if not registryService then
    self.registryIdentifier = nil
    Log:error("Servico de registro nao encontrado.\n")
    return
  end

  success, self.registryIdentifier = registryService:register(self.serviceOffer)
  if not success then
    Log:error("Erro ao registrar oferta do servico de sessao.\n")
    self.registryIdentifier = nil
    return
  end
end

---
--Finaliza o servi�o.
--
--@see scs.core.IComponent#shutdown
---
function SessionServiceComponent:shutdown()
  Log:service("Pedido de shutdown para o servi�o de sess�o")
  if not self.started then
    Log:error("Servico ja foi finalizado.\n")
    error{"IDL:SCS/ShutdownFailed:1.0"}
  end
  self.started = false

  local accessControlService = self.AccessControlServiceReceptacle
  if self.registryIdentifier then
    local registryService = Openbus:getRegistryService()
    if not registryService then
      Log:error("Servi�o de registro n�o encontrado")
    else
      registryService:unregister(self.registryIdentifier)
    end
    self.registryIdentifier = nil
  end

  if self.sessionService.observerId then
    accessControlService:removeObserver(self.sessionService.observerId)
    self.sessionService.observerId = nil
  end

  if Openbus:isConnected() then
    Openbus:disconnect()
  end

  Log:service("Servi�o de sess�o finalizado")
end
