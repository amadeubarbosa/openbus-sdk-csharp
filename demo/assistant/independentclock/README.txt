A demo IndependentClock tenta demonstrar um serviço de relógio que pode funcionar tanto conectado a um barramento, como de forma independente. O servidor imprime constantemente na tela o horário local. Em outra thread, tenta se conectar ao barramento, realizar o login e registrar sua oferta. Caso o login seja perdido, sua callback de login inválido tenta refazer esse processo eternamente até conseguir, mas a outra thread independente do barramento, que imprime a hora constantemente, continua funcionando.

O cliente, que também pode funcionar sem estar conectado ao barramento, imprime constantemente na tela a hora local. Em outra thread, tenta acessar o barramento para buscar e utilizar o servidor. Se conseguir, a thread de impressão de hora passa a imprimir na tela a hora do servidor. Caso a conexão com o barramento seja perdida, volta a imprimir a hora local até que consiga se reconectar ao barramento.

------------------------------
-------- DEPENDÊNCIAS---------
------------------------------

As dependências de software são fornecidas já compiladas, em conjunto com a demo.

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
5) tempo de espera entre cada tentativa de acesso ao barramento (em segundos e opcional - se não for fornecido, será 1)

Cliente
1) host do barramento
2) porta do barramento
3) nome de entidade
4) senha
5) tempo de espera entre cada tentativa de acesso ao barramento (em segundos e opcional - se não for fornecido, será 1)


------------------------------
---------- EXECUÇÃO ----------
------------------------------

Para que a demo funcione, pode ser necessário que as devidas permissões sejam cadastradas no barramento. Consulte o administrador do barramento e altere o arquivo admin\demo_independent_clock.adm conforme necessário.

A demo deve ser executada na seguinte ordem:

1) Servidor
2) Cliente


-------------------------------
----------- EXEMPLO -----------
-------------------------------

1) Server.exe localhost 2089 demo_independentclock_csharp_server DemoIndependentClock.key 3
2) Client.exe localhost 2089 demo_independentclock_csharp_client minhasenha 3
