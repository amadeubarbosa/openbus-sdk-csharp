--
-- Configura��o para o interceptador de requisi��es ao servi�o de registro
--
local CONF_DIR = os.getenv("CONF_DIR")
local config = 
  assert(loadfile(CONF_DIR.."/advanced/InterceptorsConfiguration.lua"))()

-- Acrescenta informa��o sobre a(s) interface(s) a ser(em) checada(s)
config.interfaces = {
  { interface = "IDL:openbusidl/rs/IRegistryService:1.0",
    excluded_ops = { }
  }
}
return config
