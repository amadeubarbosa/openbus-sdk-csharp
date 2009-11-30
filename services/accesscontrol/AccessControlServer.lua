-- $Id$

---
--Inicializa��o do Servi�o de Controle de Acesso
---
local string = string
local oil = require "oil"
local Openbus = require "openbus.Openbus"
local Log = require "openbus.util.Log"
local util = require "openbus.util.Utils"

-- Inicializa��o do n�vel de verbose do openbus.
Log:level(1)

local IDLPATH_DIR = os.getenv("IDLPATH_DIR")
if IDLPATH_DIR == nil then
  Log:error("A variavel IDLPATH_DIR nao foi definida.\n")
  os.exit(1)
end

local DATA_DIR = os.getenv("OPENBUS_DATADIR")
if DATA_DIR == nil then
  Log:error("A variavel OPENBUS_DATADIR nao foi definida.\n")
  os.exit(1)
end

-- Obt�m a configura��o do servi�o
assert(loadfile(DATA_DIR.."/conf/AccessControlServerConfiguration.lua"))()
local iconfig = assert(loadfile(DATA_DIR ..
  "/conf/advanced/ACSInterceptorsConfiguration.lua"))()

-- Parsing arguments
local usage_msg = [[
	--help                   : show this help
	--verbose                : turn ON the VERBOSE mode (show the system commands)
	--port=<port number>     : defines the service port (padr�o ]] 
								.. tostring(AccessControlServerConfiguration.hostPort) .. [[)
 NOTES:
 	The prefix '--' is optional in all options.
	So '--help' or '-help' or yet 'help' all are the same option.]]
local arguments = util.parse_args(arg,usage_msg,true)

if arguments.verbose == "" then
	oil.verbose:level(5)
else
	if AccessControlServerConfiguration.oilVerboseLevel then
  		oil.verbose:level(AccessControlServerConfiguration.oilVerboseLevel)
	end
end

if arguments.port then
	AccessControlServerConfiguration.hostPort = tonumber(arguments.port)
end

-- Define os n�veis de verbose para o OpenBus e para o OiL.
if AccessControlServerConfiguration.logLevel then
  Log:level(AccessControlServerConfiguration.logLevel)
end

local props = { host = AccessControlServerConfiguration.hostName,
  port = AccessControlServerConfiguration.hostPort}

-- Inicializa o barramento
Openbus:init(AccessControlServerConfiguration.hostName,
  AccessControlServerConfiguration.hostPort, props, iconfig)
  
Openbus:enableFaultTolerance()

local orb = Openbus:getORB()

local scs = require "scs.core.base"
local AccessControlService = require "core.services.accesscontrol.AccessControlService"

-----------------------------------------------------------------------------
-- AccessControlService Descriptions
-----------------------------------------------------------------------------

-- Facet Descriptions
local facetDescriptions = {}
facetDescriptions.IComponent          	= {}
facetDescriptions.IMetaInterface      	= {}
facetDescriptions.IAccessControlService = {}
facetDescriptions.ILeaseProvider       	= {}
facetDescriptions.IFaultTolerantService	= {}
facetDescriptions.IManagement           = {}

facetDescriptions.IComponent.name                     = "IComponent"
facetDescriptions.IComponent.interface_name           = "IDL:scs/core/IComponent:1.0"
facetDescriptions.IComponent.class                    = scs.Component
facetDescriptions.IComponent.key                      = "IC"

facetDescriptions.IMetaInterface.name                 = "IMetaInterface"
facetDescriptions.IMetaInterface.interface_name       = "IDL:scs/core/IMetaInterface:1.0"
facetDescriptions.IMetaInterface.class                = scs.MetaInterface

facetDescriptions.IAccessControlService.name            = "IAccessControlService"
facetDescriptions.IAccessControlService.interface_name  = "IDL:openbusidl/acs/IAccessControlService:1.0"
facetDescriptions.IAccessControlService.class           = AccessControlService.ACSFacet
facetDescriptions.IAccessControlService.key             = "ACS"

facetDescriptions.ILeaseProvider.name                  = "ILeaseProvider"
facetDescriptions.ILeaseProvider.interface_name        = "IDL:openbusidl/acs/ILeaseProvider:1.0"
facetDescriptions.ILeaseProvider.class                 = AccessControlService.LeaseProviderFacet
facetDescriptions.ILeaseProvider.key                   = "LP"

facetDescriptions.IFaultTolerantService.name                  = "IFaultTolerantService"
facetDescriptions.IFaultTolerantService.interface_name        = "IDL:openbusidl/ft/IFaultTolerantService:1.0"
facetDescriptions.IFaultTolerantService.class                 = AccessControlService.FaultToleranceFacet
facetDescriptions.IFaultTolerantService.key                   = "FTACS"

facetDescriptions.IManagement.name           = "IManagement"
facetDescriptions.IManagement.interface_name = "IDL:openbusidl/acs/IManagement:1.0"
facetDescriptions.IManagement.class          = AccessControlService.ManagementFacet
facetDescriptions.IManagement.key            = "MGM"

--Log:faulttolerance(facetDescriptions)

-- Receptacle Descriptions
local receptacleDescs = {}
receptacleDescs.RegistryServiceReceptacle = {}
receptacleDescs.RegistryServiceReceptacle.name           = "RegistryServiceReceptacle"
receptacleDescs.RegistryServiceReceptacle.interface_name =  "IDL:openbusidl/rs/IRegistryService:1.0"
receptacleDescs.RegistryServiceReceptacle.is_multiplex   = false

-- component id
local componentId = {}
componentId.name = "AccessControlService"
componentId.major_version = 1
componentId.minor_version = 0
componentId.patch_version = 0
componentId.platform_spec = ""

---
--Fun��o que ser� executada pelo OiL em modo protegido.
---
function main()
  -- Aloca uma thread do OiL para o orb
  Openbus:run()

  -- Cria o componente respons�vel pelo Servi�o de Controle de Acesso
  acsInst = scs.newComponent(facetDescriptions, receptacleDescs, componentId)

  -- Configura��es
  acsInst.IComponent.startup = AccessControlService.startup

  local acs = acsInst.IAccessControlService
  acs.config = AccessControlServerConfiguration
  acs.entries = {}
  acs.observers = {}
  acs.challenges = {}
  acs.loginPasswordValidators = {}

  for v,k in ipairs(AccessControlServerConfiguration.validators) do
    local validator = require(k)
    table.insert(acs.loginPasswordValidators, validator(acs.config))
  end

  -- Inicializa��o
  success, res = oil.pcall(acsInst.IComponent.startup, acsInst.IComponent)
  if not success then
    Log:error("Falha ao iniciar o servi�o de controle de acesso: "..
        tostring(res).."\n")
    os.exit(1)
  end
  Log:init("Servi�o de controle de acesso iniciado com sucesso")
end

print(oil.pcall(oil.main,main))
