A demo Audit tenta demonstrar como a cadeia de chamadas pode ser usada como meio de auditoria, mantendo um registro de quais foram as entidades que participaram de uma dada chamada. É importante notar que uma entidade não pode criar cadeias novas nem alterar cadeias existentes, mas pode reaproveitar uma cadeia anterior válida que tenha guardado.

Para demonstrar o que foi mencionado acima, o cliente faz uma chamada sayHello no proxy. O proxy, ao atender essa chamada, imprime na saída quem o chamou (cliente) e qual foi a cadeia completa da chamada, que deve conter apenas o cliente. Em seguida, ele se junta a essa cadeia e faz uma chamada sayHello no servidor. O servidor, ao atender essa chamada, imprime na saída quem o chamou (proxy) e qual foi a cadeia completa da chamada, que deve conter o cliente e o proxy.

------------------------------
-------- DEPENDÊNCIAS---------
------------------------------

As dependências de software já são fornecidas compiladas em conjunto com a demo.

Servidor:
IIOPChannel.dll
OpenBus.dll
OpenBus.Demo.Hello.Idl.dll
OpenBus.Idl
Scs.dll
Scs.Core.dll
AuditUtils.dll

Proxy:
IIOPChannel.dll
OpenBus.dll
OpenBus.Demo.Hello.Idl.dll
OpenBus.Idl
Scs.dll
Scs.Core.dll
AuditUtils.dll

Cliente:
IIOPChannel.dll
OpenBus.dll
OpenBus.Demo.Hello.Idl.dll
OpenBus.Idl
Scs.Core.dll


------------------------------
--------- ARGUMENTOS ---------
------------------------------

Servidor
1) host do barramento
2) porta do barramento
3) nome de entidade
4) caminho para a chave privada

Proxy
1) host do barramento
2) porta do barramento
3) nome de entidade
4) caminho para a chave privada
5) nome de entidade utilizado no Servidor

Cliente
1) host do barramento
2) porta do barramento
3) nome de entidade
4) senha


------------------------------
---------- EXECUÇÃO ----------
------------------------------

Para que a demo funcione, pode ser necessário que as devidas permissões sejam cadastradas no barramento. Consulte o administrador do barramento e altere o arquivo admin\demo_audit.adm conforme necessário.

A demo deve ser executada na seguinte ordem:

1) Servidor
2) Proxy
3) Cliente


-------------------------------
----------- EXEMPLO -----------
-------------------------------

1) Server.exe localhost 2089 demo_audit_csharp_server DemoAudit.key
2) Proxy.exe localhost 2089 demo_audit_csharp_proxy DemoAudit.key demo_audit_csharp_server
3) Client.exe localhost 2089 demo_audit_csharp_client minhasenha
