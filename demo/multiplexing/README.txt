A demo Multiplexing tenta demonstrar o uso da multiplexa��o com apenas uma thread e v�rias conex�es. Nela um servidor cria 3 conex�es para 3 componentes com l�nguas diferentes (similar � demo Greetings). Ela demonstra ainda a utiliza��o do gerenciador de conex�es (ConnectionManager) para escolher a conex�o de despacho e a escolha de qual conex�o deve ser usada para cada requisi��o feita.

O cliente n�o utiliza multiplexa��o, criando apenas uma conex�o e definindo-a como conex�o padr�o tanto para requisi��es como para despacho. Ele atua de forma muito similar ao cliente da demo Greetings, perguntando ao usu�rio em qual l�ngua deseja obter sauda��es. Se nenhuma l�ngua for especificada, tenta obter sauda��es em todas. De acordo com o hor�rio local, ele ent�o utiliza a faceta adequada para obter a sauda��o correta.

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

Para que a demo funcione, pode ser necess�rio que as devidas permiss�es sejam cadastradas no barramento. Consulte o administrador do barramento e altere o arquivo admin\demo_multiplexing.adm conforme necess�rio.

A demo deve ser executada na seguinte ordem:

1) Servidor
2) Cliente


-------------------------------
----------- EXEMPLO -----------
-------------------------------

1) Server.exe localhost 2089 demo_multiplexing_csharp_server DemoMultiplexing.key
2) Client.exe localhost 2089 demo_multiplexing_csharp_client minhasenha demo_multiplexing_csharp_server
