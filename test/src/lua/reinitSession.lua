--
-- Testa a reinicializa��o dos servi�o de sess�o
--
-- $Id$
--
package.loaded["oil.component"] = require "loop.component.wrapped"
package.loaded["oil.port"]      = require "loop.component.intercepted"
local oil = require "oil"

local ClientInterceptor = require "openbus.common.ClientInterceptor"
local CredentialManager = require "openbus.common.CredentialManager"
local ClientConnectionManager = require "openbus.common.ClientConnectionManager"

oil.verbose:level(3)

local CORBA_IDL_DIR = os.getenv("CORBA_IDL_DIR")
if CORBA_IDL_DIR == nil then
  error("ERRO: A variavel CORBA_IDL_DIR nao foi definida.\n")
end

oil.loadidlfile(CORBA_IDL_DIR.."/session_service.idl")
oil.loadidlfile(CORBA_IDL_DIR.."/registry_service.idl")
oil.loadidlfile(CORBA_IDL_DIR.."/access_control_service.idl")

function main()
  -- Aloca uma thread para o oil
  local success, res = oil.pcall(oil.newthread, oil.run)
  if not success then
    error("ERRO: Falha na execu��o da thread do oil")
  end

  local user = "tester"
  local password = "tester"

  -- Conecta o cliente ao barramento
  local credentialManager = CredentialManager()
  local connectionManager =
    ClientConnectionManager("localhost:2089", credentialManager, user, password)

  -- obt�m a refer�ncia para o Servi�o de Controle de Acesso
  local accessControlService = connectionManager:getAccessControlService()
  if accessControlService == nil then
    error("ERRO: N�o obteve servi�o de controle de acesso")
  end

  -- instala o interceptador de cliente
  local CONF_DIR = os.getenv("CONF_DIR")
  if CONF_DIR == nil then
    error("ERRO: A variavel CONF_DIR nao foi definida.\n")
  end
    local config = 
      assert(loadfile(CONF_DIR.."/advanced/InterceptorsConfiguration.lua"))()
    oil.setclientinterceptor(ClientInterceptor(config, credentialManager))
  
  -- autentica o cliente
  success = connectionManager:connect()
  print("Cliente autenticado!")
  
  local registryService = accessControlService:getRegistryService()
  if not registryService then
    error("ERRO: N�o obteve refer�ncia para servi�o de registro")
  end
  print("Obteve referencia para o servi�o de registro")

  print("Aguardando...\n")
  io.read()

  local offers = registryService:find("SessionService",{})
  if #offers == 0 then
    error("ERRO: N�o obteve oferta de servi�o de sess�o")
  end
  local sessionServiceComponent = oil.narrow(offers[1].member, 
                 "IDL:scs/core/IComponent:1.0")
  local sessionServiceInterface = "IDL:openbusidl/ss/ISessionService:1.0"
  local sessionService = 
    sessionServiceComponent:getFacet(sessionServiceInterface)
  sessionService = oil.narrow(sessionService, sessionServiceInterface)
  print("Obteve referencia para o servi�o de sess�o")

  sessionServiceComponent:shutdown()
  print("Shutdown do servi�o de sess�o")

  sessionServiceComponent:startup()
  print("Startup do servi�o de sess�o")

  -- desconecta o cliente do barramento
  connectionManager:disconnect()
  print("Cliente desconectado")
  os.exit(0)
end

print(oil.pcall(oil.main, main))
