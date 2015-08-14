A demo DedicatedClock tenta demonstrar um servi�o de rel�gio que n�o pode funcionar sem estar conectado a um barramento. O servidor s� funciona ap�s conseguir conectar, realizar o login e registrar sua oferta. Caso o login seja perdido, sua callback de login inv�lido tenta refazer esse processo eternamente at� conseguir.

O cliente, por sua vez, tenta acessar o barramento para buscar e utilizar o servidor. Se n�o conseguir ap�s um tempo, falha com uma mensagem de erro.

------------------------------
-------- DEPEND�NCIAS---------
------------------------------

As depend�ncias de software s�o fornecidas j� compiladas, em conjunto com a demo.

Servidor:
.NET 4.0
BouncyCastle.Crypto.dll
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
5) tempo de espera entre cada tentativa de acesso ao barramento (em segundos e opcional - se n�o for fornecido, ser� 1)

Cliente
1) host do barramento
2) porta do barramento
3) nome de entidade
4) senha (opcional - se n�o for fornecida, ser� usado o nome de entidade)
5) tempo de espera entre cada tentativa de acesso ao barramento (em segundos e opcional - se n�o for fornecido, ser� 1)
6) n�mero m�ximo de tentativas de acesso ao barramento (opcional - se n�o for fornecido, ser� 10)


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

1) Server.exe localhost 2089 demo_dedicatedclock_csharp_server DemoDedicatedClock.key 2
2) Client.exe localhost 2089 demo_dedicatedclock_csharp_client minhasenha 3 12
