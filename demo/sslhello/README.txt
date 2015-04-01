A demo SSLHello tenta demonstrar a aplica��o mais simples poss�vel, utilizando SSL diretamente na camada de transporte atrav�s do ORB. Um servidor fornece o servant da interface Hello e o cliente realiza uma chamada "sayHello" nesse servant.

------------------------------
-------- DEPEND�NCIAS---------
------------------------------

As depend�ncias de software s�o fornecidas j� compiladas, em conjunto com a demo.

Servidor:
.NET 4.0
IIOPChannel.dll
SSLPlugin.dll
Org.Mentalis.Security.dll
OpenBus.dll
OpenBus.Demo.Hello.Idl.dll
OpenBus.Idl
Scs.dll
Scs.Core.dll

Cliente:
.NET 4.0
IIOPChannel.dll
SSLPlugin.dll
Org.Mentalis.Security.dll
OpenBus.dll
OpenBus.Demo.Hello.Idl.dll
OpenBus.Idl
Scs.Core.dll


------------------------------
--------- ARGUMENTOS ---------
------------------------------

Servidor
1) caminho para arquivo com o IOR do barramento
2) nome de entidade
3) caminho para a chave privada
4) nome do usu�rio que cont�m a chave privada para realizar chamadas SSL
5) identificador (thumbprint) da chave privada no espa�o do usu�rio do Windows que a cont�m, para realizar chamadas SSL
6) nome do usu�rio que cont�m a chave privada para receber chamadas SSL
7) identificador (thumbprint) da chave privada no espa�o do usu�rio do Windows que a cont�m, para receber chamadas SSL
8) porta a ser utilizada para receber chamadas SSL

Cliente
1) caminho para arquivo com o IOR do barramento
2) dom�nio da entidade
3) nome de entidade
4) senha (opcional - se n�o for fornecida, ser� usado o nome de entidade)
5) nome do usu�rio que cont�m a chave privada para realizar chamadas SSL
6) identificador (thumbprint) da chave privada no espa�o do usu�rio do Windows que a cont�m, para realizar chamadas SSL
7) nome do usu�rio que cont�m a chave privada para receber chamadas SSL
8) identificador (thumbprint) da chave privada no espa�o do usu�rio do Windows que a cont�m, para receber chamadas SSL
9) porta a ser utilizada para receber chamadas SSL


------------------------------
---------- EXECU��O ----------
------------------------------

Para que a demo funcione, pode ser necess�rio que as devidas permiss�es sejam cadastradas no barramento. Consulte o administrador do barramento e altere o arquivo admin\demo_hello.adm da demo Hello (contido apenas na demo Hello sem SSL) conforme necess�rio.

A demo deve ser executada na seguinte ordem:

1) Servidor
2) Cliente


-------------------------------
----------- EXEMPLO -----------
-------------------------------

1) Server.exe ior.txt demo_hello_csharp_server DemoHello.key CurrentUser <thumbprint> CurrentUser <thumbprint> 58000
2) Client.exe ior.txt meudominio demo_hello_csharp_client minhasenha CurrentUser <thumbprint> CurrentUser <thumbprint> 58001
