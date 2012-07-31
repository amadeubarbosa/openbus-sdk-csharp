A demo DedicatedClock tenta demonstrar um servi�o de rel�gio que n�o pode funcionar sem estar conectado a um barramento. O servidor s� funciona ap�s conseguir conectar, realizar o login e registrar sua oferta. Caso o login seja perdido, sua callback de login inv�lido tenta refazer esse processo eternamente at� conseguir.

O cliente, por sua vez, tenta acessar o barramento para buscar e utilizar o servidor. Se n�o conseguir ap�s um tempo, falha com uma mensagem de erro.

------------------------------
-------- DEPEND�NCIAS---------
------------------------------

As depend�ncias de software s�o fornecidas j� compiladas, em conjunto com a demo.

Servidor:
.NET 4.0
IIOPChannel.dll
OpenBus.dll
OpenBus.Demo.Clock.Idl.dll
OpenBus.Idl
Scs.dll
Scs.Core.dll

Cliente:
.NET 4.0
IIOPChannel.dll
OpenBus.dll
OpenBus.Demo.Clock.Idl.dll
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
5) tempo de espera entre cada tentativa de acesso ao barramento (em milisegundos)

Cliente
1) host do barramento
2) porta do barramento
3) nome de entidade
4) senha
5) nome de entidade utilizado no Servidor
6) tempo de espera entre cada tentativa de acesso ao barramento (em milisegundos)
7) tempo de espera total m�ximo (em milisegundos)


------------------------------
---------- EXECU��O ----------
------------------------------

Para que a demo funcione, pode ser necess�rio que as devidas permiss�es sejam cadastradas no barramento. Consulte o administrador do barramento e altere o arquivo admin\demo_dedicated_clock.adm conforme necess�rio.

A demo deve ser executada na seguinte ordem:

1) Servidor
2) Cliente


-------------------------------
----------- EXEMPLO -----------
-------------------------------

1) Server.exe localhost 2089 demo_dedicatedclock_csharp_server DemoDedicatedClock.key 2000
2) Client.exe localhost 2089 demo_dedicatedclock_csharp_client minhasenha demo_dedicatedclock_csharp_server 3000 12000
