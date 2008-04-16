-- $Id$

local os = os

local loadfile = loadfile
local assert = assert

local oil = require "oil"

local SessionService = require "core.services.session.SessionService"
local ClientInterceptor = require "openbus.common.ClientInterceptor"
local ServerInterceptor = require "openbus.common.ServerInterceptor"
local CredentialManager = require "openbus.common.CredentialManager"
local ServerConnectionManager = require "openbus.common.ServerConnectionManager"

local Log = require "openbus.common.Log"

local IComponent = require "scs.core.IComponent"

local oop = require "loop.simple"

---
--Componente (membro) respons�vel pelo Servi�o de Sess�o.
---
module("core.services.session.SessionServiceComponent")

oop.class(_M, IComponent)

---
--Cria um Servi�o de Sess�o.
--
--@param name O nome do componente.
--@param config As configura��es do componente.
--
--@return O Servi�o de Sess�o.
---
function __init(self, name, config)
  local component = IComponent:__init(name, 1)
  component.config = config
  return oop.rawnew(self, component)
end

---
--Inicia o componente.
--
--@see scs.core.IComponent#startup
---
function startup(self)
  Log:service("Pedido de startup para o servi�o de sess�o")

  -- Se � o primeiro startup, deve instanciar ConnectionManager e
  -- instalar interceptadores
  if not self.initialized then
    Log:service("Servi�o de sess�o est� inicializando")
    local credentialManager = CredentialManager()
    self.connectionManager =
      ServerConnectionManager(self.config.accessControlServerHost,
        credentialManager, self.config.privateKeyFile,
        self.config.accessControlServiceCertificateFile)

    -- obt�m a refer�ncia para o Servi�o de Controle de Acesso
    self.accessControlService = self.connectionManager:getAccessControlService()
    if self.accessControlService == nil then
      error{"IDL:SCS/StartupFailed:1.0"}
    end

    -- instala o interceptador cliente
    local CONF_DIR = os.getenv("CONF_DIR")
    local interceptorsConfig =
      assert(loadfile(CONF_DIR.."/advanced/SSInterceptorsConfiguration.lua"))()
    oil.setclientinterceptor(
      ClientInterceptor(interceptorsConfig, credentialManager))

    -- instala o interceptador servidor
    self.serverInterceptor = ServerInterceptor(interceptorsConfig, self.accessControlService)
    oil.setserverinterceptor(self.serverInterceptor)

    self.initialized = true
  else
    Log:service("Servi�o de sess�o j� foi inicializado")
  end

  -- autentica o servi�o, conectando-o ao barramento
  local success = self.connectionManager:connect(self.componentId.name,
      function() self.wasReconnected(self) end)
  if not success then
    error{"IDL:SCS/StartupFailed:1.0"}
  end

  -- cria e instala a faceta servidora
  self.sessionService = SessionService(self.accessControlService,
                                       self.serverInterceptor)
  local sessionServiceInterface = "IDL:openbusidl/ss/ISessionService:1.0"
  self:addFacet("sessionService", sessionServiceInterface, self.sessionService)

  -- registra sua oferta de servi�o junto ao Servi�o de Registro
  self.offerType = self.config.sessionServiceOfferType
  self.serviceOffer = {
    type = self.offerType,
    description = "Servico de Sessoes",
    properties = {},
    member = self,
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
function wasReconnected(self)
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
function shutdown(self)
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

  self:removeFacets()
  Log:service("Servi�o de sess�o finalizado")
end
