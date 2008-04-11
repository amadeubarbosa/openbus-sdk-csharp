-- $Id$

---
--Inicializa��o do Servi�o de Controle de Acesso
---

-- Habilitando o suporte a interceptadores
package.loaded["oil.component"] = require "loop.component.wrapped"
package.loaded["oil.port"]      = require "loop.component.intercepted"

local oil = require "oil"

local Log = require "openbus.common.Log"

local AccessControlService =
    require "core.services.accesscontrol.AccessControlService"

local CORE_IDL_DIR = os.getenv("CORE_IDL_DIR")
if CORE_IDL_DIR == nil then
  Log:error("A variavel CORE_IDL_DIR nao foi definida.\n")
  os.exit(1)
end

local CONF_DIR = os.getenv("CONF_DIR")
if CONF_DIR == nil then
  Log:error("A variavel CONF_DIR nao foi definida.\n")
  os.exit(1)
end

-- Obt�m a configura��o do servi�o
assert(loadfile(CONF_DIR.."/AccessControlServerConfiguration.lua"))()

-- Define os n�veis de verbose para o OpenBus e para o OiL.
if AccessControlServerConfiguration.logLevel then
  Log:level(AccessControlServerConfiguration.logLevel)
end
if AccessControlServerConfiguration.oilVerboseLevel then
  oil.verbose:level(AccessControlServerConfiguration.oilVerboseLevel)
end

oil.loadidlfile(CORE_IDL_DIR.."/access_control_service.idl")

-- Inicializa o ORB, fixando a localiza��o do servi�o em uma porta espec�fica
oil.init{host = AccessControlServerConfiguration.hostName,
    port = AccessControlServerConfiguration.hostPort}

---
--Fun��o que ser� executada pelo OiL em modo protegido.
---
function main()
  local success, res = oil.pcall(oil.newthread, oil.run)
  if not success then
    Log:error("Falha na execu��o do ORB: "..tostring(res).."\n")
    os.exit(1)
  end

  -- Cria o componente respons�vel pelo Servi�o de Controle de Acesso
  success, res  = oil.pcall(oil.newservant,
      AccessControlService("AccessControlService",
      AccessControlServerConfiguration),
      "IDL:openbusidl/acs/IAccessControlService:1.0", "ACS")
  if not success then
    Log:error("Falha criando o AcessControlService: "..tostring(res).."\n")
    os.exit(1)
  end

  local accessControlService = res
  success, res = oil.pcall(accessControlService.startup, accessControlService)
  if not success then
    Log:error("Falha ao iniciar o servi�o de controle de acesso: "..
        tostring(res).."\n")
    os.exit(1)
  end
  Log:init("Servi�o de controle de acesso iniciado com sucesso")
end

print(oil.pcall(oil.main,main))
