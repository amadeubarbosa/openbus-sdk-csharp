A demo CallChain tenta demonstrar como a cadeia de chamadas pode ser usada como meio de valida��o para permitir certas opera��es. Esta demo conta com um servidor, um proxy e um cliente. O servidor aceita mensagens, mas apenas se a �ltima entidade da cadeia ("caller") for a mesma entidade (caso do proxy). O proxy aceita mensagens de qualquer um, e repassa a mensagem para o servidor.

------------------------------
-------- DEPEND�NCIAS---------
------------------------------

As depend�ncias de software s�o fornecidas j� compiladas, em conjunto com a demo.

Servidor:
.NET 4.0
BouncyCastle.Crypto.dll
IIOPChannel.dll
OpenBus.dll
OpenBus.Demo.CallChain.Idl.dll
OpenBus.Idl
Scs.dll
Scs.Core.dll

Proxy:
.NET 4.0
BouncyCastle.Crypto.dll
IIOPChannel.dll
OpenBus.dll
OpenBus.Demo.CallChain.Idl.dll
OpenBus.Idl
Scs.dll
Scs.Core.dll

Cliente:
.NET 4.0
IIOPChannel.dll
OpenBus.dll
OpenBus.Demo.CallChain.Idl.dll
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

Cliente
1) host do barramento
2) porta do barramento
3) dom�nio da entidade
4) nome de entidade
5) senha (opcional - se n�o for fornecida, ser� usado o nome de entidade)


------------------------------
---------- EXECU��O ----------
------------------------------

Para que a demo funcione, pode ser necess�rio que as devidas permiss�es sejam cadastradas no barramento. Consulte o administrador do barramento e altere o arquivo admin\demo_callchain.adm conforme necess�rio.

A demo deve ser executada na seguinte ordem:

1) Servidor
2) Proxy
3) Cliente


-------------------------------
----------- EXEMPLO -----------
-------------------------------

1) Server.exe localhost 2089 demo_callchain_csharp_server DemoCallChain.key
2) Proxy.exe localhost 2089 demo_callchain_csharp_server DemoCallChain.key
3) Client.exe localhost 2089 meudominio demo_callchain_csharp_client minhasenha
