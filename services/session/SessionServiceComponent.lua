-- $Id$

local os = os

local loadfile = loadfile
local assert = assert
local error = error
local string = string

local oil = require "oil"
local orb = oil.orb

local SessionService = require "core.services.session.SessionService"
local ClientInterceptor = require "openbus.interceptors.ClientInterceptor"
local ServerInterceptor = require "openbus.interceptors.ServerInterceptor"
local CredentialManager = require "openbus.util.CredentialManager"
local ServerConnectionManager = require "openbus.common.ServerConnectionManager"

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

  -- Se � o primeiro startup, deve instanciar ConnectionManager e
  -- instalar interceptadores
  if not self.initialized then
    Log:service("Servi�o de sess�o est� inicializando")
    local credentialManager = CredentialManager()
    local privateKeyFile
    if (string.sub(self.config.privateKeyFile,1 , 1) == "/") then
      privateKeyFile = self.config.privateKeyFile
    else
      privateKeyFile = DATA_DIR.."/"..self.config.privateKeyFile
    end
    local accessControlServiceCertificateFile
    if (string.sub(self.config.accessControlServiceCertificateFile,1 , 1) == "/") then
      accessControlServiceCertificateFile = self.config.accessControlServiceCertificateFile
    else
      accessControlServiceCertificateFile = DATA_DIR.."/"..self.config.accessControlServiceCertificateFile
    end
    self.connectionManager =
      ServerConnectionManager(self.config.accessControlServerHost,
        credentialManager, privateKeyFile,
        accessControlServiceCertificateFile)

    -- obt�m a refer�ncia para o Servi�o de Controle de Acesso
    self.accessControlService = self.connectionManager:getAccessControlService()
    if self.accessControlService == nil then
      error{"IDL:SCS/StartupFailed:1.0"}
    end

    -- instala o interceptador cliente
    local interceptorsConfig =
      assert(loadfile(DATA_DIR.."/conf/advanced/SSInterceptorsConfiguration.lua"))()
    orb:setclientinterceptor(
      ClientInterceptor(interceptorsConfig, credentialManager))

    -- instala o interceptador servidor
    self.serverInterceptor = ServerInterceptor(interceptorsConfig, self.accessControlService)
    orb:setserverinterceptor(self.serverInterceptor)

    self.initialized = true
  else
    Log:service("Servi�o de sess�o j� foi inicializado")
  end

  -- autentica o servi�o, conectando-o ao barramento
  local success = self.connectionManager:connect(self.context._componentId.name,
      function() self.wasReconnected(self) end)
  if not success then
    error{"IDL:SCS/StartupFailed:1.0"}
  end

  -- configura faceta ISessionService
  self.sessionService = self.context.ISessionService
  self.sessionService.serverInterceptor = self.serverInterceptor
  self.sessionService.accessControlService = self.accessControlService

  -- registra sua oferta de servi�o junto ao Servi�o de Registro
  self.serviceOffer = {
    member = self.context.IComponent,
    properties = {
      {name = "facets", value = {"sessionService"}},
    },
  }
  local registryService = self.accessControlService:getRegistryService()
  if not registryService then
    Log:error("Servico de registro nao encontrado.\n")
    self.connectionManager:disconnect()
    error{"IDL:SCS/StartupFailed:1.0"}
  end

  success, self.registryIdentifier = registryService:register(self.serviceOffer)
  if not success then
    Log:error("Erro ao registrar oferta do servico de sessao.\n")
    self.connectionManager:disconntect()
    error{"IDL:SCS/StartupFailed:1.0"}
  end

  self.started = true
  Log:service("Servi�o de sess�o iniciado")
end

---
--Procedimento ap�s a reconex�o do servi�o.
---
function SessionServiceComponent:wasReconnected()
Log:service("Servi�o de sess�o foi reconectado")

  -- Procedimento realizado pela faceta
  self.sessionService:wasReconnected()

  -- Registra novamente a oferta de servi�o, pois a credencial associada
  -- agora � outra
  local registryService = self.accessControlService:getRegistryService()
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

  if self.registryIdentifier then
    local accessControlService = self.connectionManager:getAccessControlService()
    local registryService = accessControlService:getRegistryService()
    if not registryService then
      Log:error("Servi�o de registro n�o encontrado")
    else
      registryService:unregister(self.registryIdentifier)
    end
    self.registryIdentifier = nil
  end

  self.sessionService:shutdown()

  self.connectionManager:disconnect()

  Log:service("Servi�o de sess�o finalizado")
end
