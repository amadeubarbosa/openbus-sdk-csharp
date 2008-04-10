-----------------------------------------------------------------------------
-- Inicializa��o do Servi�o de Sess�o
--
-- �ltima altera��o:
--   $Id$
-----------------------------------------------------------------------------
package.loaded["oil.component"] = require "loop.component.wrapped"
package.loaded["oil.port"]      = require "loop.component.intercepted"
local oil = require "oil"

local Log = require "openbus.common.Log"

local SessionServiceComponent =
    require "openbus.services.session.SessionServiceComponent"

local CORBA_IDL_DIR = os.getenv("CORBA_IDL_DIR")
if CORBA_IDL_DIR == nil then
  Log:error("A variavel CORBA_IDL_DIR nao foi definida.\n")
  os.exit(1)
end

local CONF_DIR = os.getenv("CONF_DIR")
if CONF_DIR == nil then
  Log:error("A variavel CONF_DIR nao foi definida.\n")
  os.exit(1)
end

-- Obt�m a configura��o do servi�o
assert(loadfile(CONF_DIR.."/SessionServerConfiguration.lua"))()

SessionServerConfiguration.accessControlServerHost =
    SessionServerConfiguration.accessControlServerHostName..":"..
    SessionServerConfiguration.accessControlServerHostPort

-- Seta os n�veis de verbose para o openbus e para o oil
if SessionServerConfiguration.logLevel then
  Log:level(SessionServerConfiguration.logLevel)
end
if SessionServerConfiguration.oilVerboseLevel then
  oil.verbose:level(SessionServerConfiguration.oilVerboseLevel)
end

-- Carrega a interface do servi�o
local idlfile = CORBA_IDL_DIR.."/session_service.idl"
oil.loadidlfile (idlfile)
idlfile = CORBA_IDL_DIR.."/access_control_service.idl"
oil.loadidlfile (idlfile)
idlfile = CORBA_IDL_DIR.."/registry_service.idl"
oil.loadidlfile (idlfile)

function main()
  -- Aloca uma thread para o orb
  local success, res = oil.pcall(oil.newthread, oil.run)
  if not success then
    Log:error("Falha na execu��o do ORB: "..tostring(res).."\n")
    os.exit(1)
  end

  print("ABCD")
  -- Cria o componente respons�vel pelo Servi�o de Sess�o
  success, res = oil.pcall(oil.newservant,
      SessionServiceComponent("SessionService", SessionServerConfiguration),
      "IDL:scs/core/IComponent:1.0")
  print("EFGH")
  if not success then
    Log:error("Falha criando SessionServiceComponent: "..tostring(res).."\n")
    os.exit(1)
  end
  local sessionServiceComponent = res
  success, res = oil.pcall(sessionServiceComponent.startup,
      sessionServiceComponent)
  if not success then
    Log:error("Falha ao iniciar o servi�o de sess�o: "..tostring(res).."\n")
    os.exit(1)
  end
  Log:init("Servi�o de sess�o iniciado com sucesso")
end

print(oil.pcall(oil.main,main))
