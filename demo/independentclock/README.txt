A demo IndependentClock tenta demonstrar um servi�o de rel�gio que pode funcionar tanto conectado a um barramento, como de forma independente. O servidor imprime constantemente na tela o hor�rio local. Em outra thread, tenta se conectar ao barramento, realizar o login e registrar sua oferta. Caso o login seja perdido, sua callback de login inv�lido tenta refazer esse processo eternamente at� conseguir, mas a outra thread independente do barramento, que imprime a hora constantemente, continua funcionando.

O cliente, que tamb�m pode funcionar sem estar conectado ao barramento, imprime constantemente na tela a hora local. Em outra thread, tenta acessar o barramento para buscar e utilizar o servidor. Se conseguir, a thread de impress�o de hora passa a imprimir na tela a hora do servidor. Caso a conex�o com o barramento seja perdida, volta a imprimir a hora local at� que consiga se reconectar ao barramento.

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
5) tempo de espera entre cada tentativa de acesso ao barramento (em milisegundos e opcional - se n�o for fornecido, ser� 1)

Cliente
1) host do barramento
2) porta do barramento
3) dom�nio da entidade
4) nome de entidade
5) senha
6) tempo de espera entre cada tentativa de acesso ao barramento (em milisegundos e opcional - se n�o for fornecido, ser� 1)


------------------------------
---------- EXECU��O ----------
------------------------------

Para que a demo funcione, pode ser necess�rio que as devidas permiss�es sejam cadastradas no barramento. Consulte o administrador do barramento e altere o arquivo admin\demo_independent_clock.adm conforme necess�rio.

A demo deve ser executada na seguinte ordem:

1) Servidor
2) Cliente


-------------------------------
----------- EXEMPLO -----------
-------------------------------

1) Server.exe localhost 2089 demo_independentclock_csharp_server DemoIndependentClock.key 3000
2) Client.exe localhost 2089 meudominio demo_independentclock_csharp_client minhasenha 3000
