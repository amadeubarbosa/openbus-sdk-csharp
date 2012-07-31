A demo Greetings tenta demonstrar o uso de v�rios componentes e facetas. O servidor cria 3 componentes, um para cada l�ngua suportada (ingl�s, espanhol e portugu�s). Cada componente tem 3 facetas: uma responde "bom dia", uma "boa tarde" e outra "boa noite", na l�ngua que estiver em uso. Apenas uma implementa��o do servant da interface Greetings � necess�ria.

O cliente pergunta ao usu�rio em qual l�ngua deseja obter sauda��es. Se nenhuma l�ngua for especificada, tentar� obter sauda��es em todas. De acordo com o hor�rio local, ele ent�o utiliza a faceta adequada para obter a sauda��o correta.

------------------------------
-------- DEPEND�NCIAS---------
------------------------------

As depend�ncias de software s�o fornecidas j� compiladas, em conjunto com a demo.

Servidor:
.NET 4.0
IIOPChannel.dll
OpenBus.dll
OpenBus.Demo.Greetings.Idl.dll
OpenBus.Idl
Scs.dll
Scs.Core.dll

Cliente:
.NET 4.0
IIOPChannel.dll
OpenBus.dll
OpenBus.Demo.Greetings.Idl.dll
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

Cliente
1) host do barramento
2) porta do barramento
3) nome de entidade
4) senha
5) nome de entidade utilizado no Servidor


------------------------------
---------- EXECU��O ----------
------------------------------

Para que a demo funcione, pode ser necess�rio que as devidas permiss�es sejam cadastradas no barramento. Consulte o administrador do barramento e altere o arquivo admin\demo_greetings.adm conforme necess�rio.

A demo deve ser executada na seguinte ordem:

1) Servidor
2) Cliente


-------------------------------
----------- EXEMPLO -----------
-------------------------------

1) Server.exe localhost 2089 demo_greetings_csharp_server DemoGreetings.key
2) Client.exe localhost 2089 demo_greetings_csharp_client minhasenha demo_greetings_csharp_server
