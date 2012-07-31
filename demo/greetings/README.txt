A demo Greetings tenta demonstrar o uso de vários componentes e facetas. O servidor cria 3 componentes, um para cada língua suportada (inglês, espanhol e português). Cada componente tem 3 facetas: uma responde "bom dia", uma "boa tarde" e outra "boa noite", na língua que estiver em uso. Apenas uma implementação do servant da interface Greetings é necessária.

O cliente pergunta ao usuário em qual língua deseja obter saudações. Se nenhuma língua for especificada, tentará obter saudações em todas. De acordo com o horário local, ele então utiliza a faceta adequada para obter a saudação correta.

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

Para que a demo funcione, pode ser necessário que as devidas permissões sejam cadastradas no barramento. Consulte o administrador do barramento e altere o arquivo admin\demo_greetings.adm conforme necessário.

A demo deve ser executada na seguinte ordem:

1) Servidor
2) Cliente


-------------------------------
----------- EXEMPLO -----------
-------------------------------

1) Server.exe localhost 2089 demo_greetings_csharp_server DemoGreetings.key
2) Client.exe localhost 2089 demo_greetings_csharp_client minhasenha demo_greetings_csharp_server
