A demo Multiplexing tenta demonstrar o uso da multiplexação com apenas uma thread e várias conexões. Nela um servidor cria 3 conexões para 3 componentes com línguas diferentes (similar à demo Greetings). Ela demonstra ainda a utilização do gerenciador de conexões (ConnectionManager) para escolher a conexão de despacho e a escolha de qual conexão deve ser usada para cada requisição feita.

O cliente não utiliza multiplexação, criando apenas uma conexão e definindo-a como conexão padrão tanto para requisições como para despacho. Ele atua de forma muito similar ao cliente da demo Greetings, perguntando ao usuário em qual língua deseja obter saudações. Se nenhuma língua for especificada, tenta obter saudações em todas. De acordo com o horário local, ele então utiliza a faceta adequada para obter a saudação correta.

------------------------------
-------- DEPENDÊNCIAS---------
------------------------------

As dependências de software são fornecidas já compiladas, em conjunto com a demo.

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
---------- EXECUÇÃO ----------
------------------------------

Para que a demo funcione, pode ser necessário que as devidas permissões sejam cadastradas no barramento. Consulte o administrador do barramento e altere o arquivo admin\demo_multiplexing.adm conforme necessário.

A demo deve ser executada na seguinte ordem:

1) Servidor
2) Cliente


-------------------------------
----------- EXEMPLO -----------
-------------------------------

1) Server.exe localhost 2089 demo_multiplexing_csharp_server DemoMultiplexing.key
2) Client.exe localhost 2089 demo_multiplexing_csharp_client minhasenha demo_multiplexing_csharp_server
