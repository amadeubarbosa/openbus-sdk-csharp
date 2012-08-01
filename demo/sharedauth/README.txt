A demo Shared Auth tenta demonstrar como se realiza o login por autenticação compartilhada. Um servidor fornece o servant da interface Hello e um cliente realiza uma chamada "sayHello" nesse servant. Além disso, esse cliente inicia o processo de login por autenticação, recebendo um objeto LoginProcess e um segredo, que codifica em CDR utilizando uma estrutura definida em IDL. Por fim, guarda essa informação codificada em um arquivo.

Um outro cliente, que fará o login por autenticação compartilhada, lê então esse arquivo para obter o objeto LoginProcess e o segredo. De posse desses dados, consegue realizar o login, procurar pelo serviço Hello e realizar também uma chamada "sayHello", com a mesma entidade mas um identificador de login diferente.

É importante notar que o tempo entre a iniciação do processo de login por autenticação compartilhada do primeiro cliente e o login de fato do segundo cliente deve ser menor que o tempo de lease. Caso contrário, o login expirará e uma exceção será recebida.

------------------------------
-------- DEPENDÊNCIAS---------
------------------------------

As dependências de software são fornecidas já compiladas, em conjunto com a demo.

Servidor:
.NET 4.0
IIOPChannel.dll
OpenBus.dll
OpenBus.Demo.Hello.Idl.dll
OpenBus.Idl
Scs.dll
Scs.Core.dll

Cliente:
.NET 4.0
IIOPChannel.dll
OpenBus.dll
OpenBus.Demo.Hello.Idl.dll
OpenBus.Demo.SharedAuth.Idl.dll
OpenBus.Idl
Scs.Core.dll

Cliente SharedAuth:
.NET 4.0
IIOPChannel.dll
OpenBus.dll
OpenBus.Demo.Hello.Idl.dll
OpenBus.Demo.SharedAuth.Idl.dll
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
6) caminho para o arquivo onde serão escritos os dados da autenticação compartilhada

Cliente SharedAuth:
1) host do barramento
2) porta do barramento
3) nome de entidade utilizado no Servidor
4) caminho para o arquivo com os dados da autenticação compartilhada


------------------------------
---------- EXECUÇÃO ----------
------------------------------

Para que a demo funcione, pode ser necessário que as devidas permissões sejam cadastradas no barramento. Consulte o administrador do barramento e altere o arquivo admin\demo_sharedauth.adm conforme necessário.

A demo deve ser executada na seguinte ordem:

1) Servidor
2) Cliente
3) Cliente SharedAuth


-------------------------------
----------- EXEMPLO -----------
-------------------------------

1) Server.exe localhost 2089 demo_sharedauth_csharp_server DemoSharedAuth.key
2) Client.exe localhost 2089 demo_sharedauth_csharp_client minhasenha demo_sharedauth_csharp_server login.bin
3) SharedAuthClient.exe localhost 2089 demo_sharedauth_csharp_server login.bin
