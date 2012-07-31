A demo ChainValidation tenta demonstrar como a cadeia de chamadas pode ser usada como meio de valida��o para permitir certas opera��es. Esta demo conta com um executivo, sua secret�ria e um cliente. O executivo aceita mensagens, mas apenas se a secret�ria for a �ltima entidade da cadeia ("caller"). A secret�ria aceita mensagens de qualquer um, e tamb�m pedidos de agendamento de reuni�o com o executivo. Ao receber um pedido desses, envia uma mensagem para o executivo, informando-o.

------------------------------
-------- DEPEND�NCIAS---------
------------------------------

As depend�ncias de software s�o fornecidas j� compiladas, em conjunto com a demo.

Executivo:
.NET 4.0
IIOPChannel.dll
OpenBus.dll
OpenBus.Demo.ChainValidation.Idl.dll
OpenBus.Idl
Scs.dll
Scs.Core.dll

Secret�ria:
.NET 4.0
IIOPChannel.dll
OpenBus.dll
OpenBus.Demo.ChainValidation.Idl.dll
OpenBus.Idl
Scs.dll
Scs.Core.dll

Cliente:
.NET 4.0
IIOPChannel.dll
OpenBus.dll
OpenBus.Demo.ChainValidation.Idl.dll
OpenBus.Idl
Scs.Core.dll


------------------------------
--------- ARGUMENTOS ---------
------------------------------

Executivo
1) host do barramento
2) porta do barramento
3) nome de entidade
4) caminho para a chave privada
5) nome de entidade utilizado na Secret�ria

Secret�ria
1) host do barramento
2) porta do barramento
3) nome de entidade
4) caminho para a chave privada
5) nome de entidade utilizado no Executivo

Cliente
1) host do barramento
2) porta do barramento
3) nome de entidade
4) senha


------------------------------
---------- EXECU��O ----------
------------------------------

Para que a demo funcione, pode ser necess�rio que as devidas permiss�es sejam cadastradas no barramento. Consulte o administrador do barramento e altere o arquivo admin\demo_chainvalidation.adm conforme necess�rio.

A demo deve ser executada na seguinte ordem:

1) Executivo
2) Secret�ria
3) Cliente


-------------------------------
----------- EXEMPLO -----------
-------------------------------

1) Executive.exe localhost 2089 demo_chainvalidation_csharp_executive DemoChainValidation.key
2) Secretary.exe localhost 2089 demo_chainvalidation_csharp_secretary DemoChainValidation.key demo_chainvalidation_csharp_executive
3) Client.exe localhost 2089 demo_audit_chainvalidation_client minhasenha
