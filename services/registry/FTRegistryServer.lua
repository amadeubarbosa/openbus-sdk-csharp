-----------------------------------------------------------------------------
-- Inicializa��o do Servi�o de Registro Tolerante a Falhas
--
--   $Id: FTRegistryServer.lua 
-----------------------------------------------------------------------------
local tonumber = tonumber

local oil = require "oil"
local Openbus = require "openbus.Openbus"
local Log = require "openbus.util.Log"

-- Inicializa��o do n�vel de verbose do openbus.
Log:level(1)

local DATA_DIR = os.getenv("OPENBUS_DATADIR")
if DATA_DIR == nil then
  Log:error("A variavel OPENBUS_DATADIR nao foi definida.\n")
  os.exit(1)
end

-- Obt�m a configura��o do servi�o
assert(loadfile(DATA_DIR.."/conf/RegistryServerConfiguration.lua"))()
local iConfig =
  assert(loadfile(DATA_DIR.."/conf/advanced/RSInterceptorsConfiguration.lua"))()

-- Define os n�veis de verbose para o openbus e para o oil
if RegistryServerConfiguration.logLevel then
  Log:level(RegistryServerConfiguration.logLevel)
end
if RegistryServerConfiguration.oilVerboseLevel then
  oil.verbose:level(RegistryServerConfiguration.oilVerboseLevel)
end

local hostPort = arg[1]
if hostPort == nil then
   Log:error("� necessario passar o numero da porta.\n")
    os.exit(1)
end
RegistryServerConfiguration.registryServerHostPort = tonumber(hostPort)

props = {  host = RegistryServerConfiguration.registryServerHostName,
           port =  tonumber(RegistryServerConfiguration.registryServerHostPort)}
           
-- Inicializa o barramento
Openbus:resetAndInitialize(
  RegistryServerConfiguration.accessControlServerHostName,
  RegistryServerConfiguration.accessControlServerHostPort,
  props, iConfig, iConfig)

Openbus:enableFaultTolerance()

local orb = Openbus:getORB()

local scs = require "scs.core.base"
local RegistryService = require "core.services.registry.RegistryService"
local FaultTolerantService = require "core.services.faulttolerance.FaultTolerantService"



-----------------------------------------------------------------------------
---- RegistryService Descriptions
-------------------------------------------------------------------------------

---- Facet Descriptions
local facetDescriptions = {}
facetDescriptions.IComponent       = {}
facetDescriptions.IMetaInterface   = {}
facetDescriptions.IRegistryService = {}
facetDescriptions.IFaultTolerantService	= {}
facetDescriptions.IManagement      = {}

facetDescriptions.IComponent.name                  = "IComponent"
facetDescriptions.IComponent.interface_name        = "IDL:scs/core/IComponent:1.0"
facetDescriptions.IComponent.class                 = scs.Component

facetDescriptions.IMetaInterface.name              = "IMetaInterface"
facetDescriptions.IMetaInterface.interface_name    = "IDL:scs/core/IMetaInterface:1.0"
facetDescriptions.IMetaInterface.class             = scs.MetaInterface

facetDescriptions.IRegistryService.name            = "IRegistryService"
facetDescriptions.IRegistryService.interface_name  = "IDL:openbusidl/rs/IRegistryService:1.0"
facetDescriptions.IRegistryService.class           = RegistryService.RSFacet

facetDescriptions.IFaultTolerantService.name                  = "IFaultTolerantService"
facetDescriptions.IFaultTolerantService.interface_name        = "IDL:openbusidl/ft/IFaultTolerantService:1.0"
facetDescriptions.IFaultTolerantService.class                 = FaultTolerantService.FaultToleranceFacet
facetDescriptions.IFaultTolerantService.key                   = "FTRS"

facetDescriptions.IManagement.name           = "IManagement"
facetDescriptions.IManagement.interface_name = "IDL:openbusidl/rs/IManagement:1.0"
facetDescriptions.IManagement.class          = RegistryService.ManagementFacet
facetDescriptions.IManagement.key            = "MGM"

---- Receptacle Descriptions
local receptacleDescs = {}

---- component id
local componentId = {}
componentId.name = "RegistryService"
componentId.major_version = 1
componentId.minor_version = 0
componentId.patch_version = 0
componentId.platform_spec = ""

function main()
  -- Aloca uma thread do OiL para o orb
  Openbus:run()

  -- Cria o componente respons�vel pelo Servi�o de Registro
  rsInst = scs.newComponent(facetDescriptions, receptacleDescs, componentId)

  -- Configuracoes
  rsInst.IComponent.startup = RegistryService.startup
  rsInst.IComponent.shutdown = RegistryService.shutdown

  local rs = rsInst.IRegistryService
  rs.config = RegistryServerConfiguration

  success, res = oil.pcall (rsInst.IComponent.startup, rsInst.IComponent)
  if not success then
    Log:error("Falha ao iniciar o servi�o de registro: "..tostring(res).."\n")
    os.exit(1)
  end
  Log:init("Servi�o de registro iniciado com sucesso")
end

print(oil.pcall(oil.main,main))
