@echo off

set COMPILER=%CD%\bin\IDLToCLSCompiler.exe

if not exist %COMPILER% (
  echo ERRO: IDLToCLSCompiler.exe nao foi encontrado. 
  echo Executavel = %COMPILER% 
  echo Por favor, copie o compilador IDLToCLSCompiler.exe para a pasta bin incluindo a biblioteca IIOPChannel e tente novamente.
  set /p ENTER=Pressione qualquer tecla para continuar...
  exit /b
)

@echo on

cd lib

%COMPILER% -snk ..\Openbus.snk OpenBus.Interop.Hello.Idl ..\interop\hello\idl\hello.idl

%COMPILER% -snk ..\Openbus.snk OpenBus.Test.Idl ..\Test\idl\test.idl

cd ..

