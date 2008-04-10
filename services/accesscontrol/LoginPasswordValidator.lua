-- $Id$

local oop = require "loop.base"

---
--Representa um validador de usu�rio e senha.
---
module("openbus.services.accesscontrol.LoginPasswordValidator", oop.class)

---
--Verifica se um determinado login pode acessar o sistema.
--
--@param name O login do usu�rio.
--@param password A senha do usu�rio.
--
--@return true caso o login seja v�lido, ou false e uma mensagem de erro, caso
--contr�rio.
---
function validate(self, name, password)
  return false
end
