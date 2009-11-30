-- $Id$

local LoginPasswordValidator =
    require "core.services.accesscontrol.LoginPasswordValidator"

local oop = require "loop.simple"

---
--Representa um validador de usu�rio e senha para testes.
---
module("core.services.accesscontrol.TestLoginPasswordValidator")
oop.class(_M, LoginPasswordValidator)


---
--Cria o validador LDAP.
--
--@param config As configura��es do AcessControlService.
--
--@return O validador.
---
function __init(self, config)
  return oop.rawnew(self, {
    config = config,
  })
end

---
--@see core.services.accesscontrol.LoginPasswordValidator#validate
---
function validate(self, name, password)
  if name == "tester" and password == "tester" then
    return true
  end
  return false, "O usu�rio "..name.." � desconhecido."
end
