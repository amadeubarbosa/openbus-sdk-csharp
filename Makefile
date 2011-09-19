MSBUILD=msbuild /nologo
BUILD=$(MSBUILD) /p:Configuration=Release /verbosity:minimal
CLEAN=$(MSBUILD) /p:Configuration=Release /verbosity:minimal /target:Clean
_IDLCOMPILER="%IIOP_TOOLS%\IDLToCLSCompiler.exe"
IDLCOMPILER="$(_IDLCOMPILER)"
_TEST="%VS90COMNTOOLS%..\IDE\MSTest.exe"
TEST="$(_TEST)" /nologo /noresults

VERSION=1.5.3

build: idl build-base build-examples
test: build run-tests
clean: clean-idl clean-idl-example clean-base clean-example
rebuild: clean build
all: build test
idl: compile-idl compile-idl-example


compile-idl:
    cd lib\generated
    $(IDLCOMPILER) -snk ..\..\Openbus.snk -asmVersion $(VERSION) -r:..\Scs.Core.dll \
 Openbus.Idl ..\..\idl\access_control_service.idl ..\..\idl\registry_service.idl \
 ..\..\idl\session_service.idl ..\..\idl\core.idl
    cd ..\..

compile-idl-example:
    cd demo\delegate\lib\generated
    $(IDLCOMPILER) -asmVersion $(VERSION) Openbus.Demo.Delegate.Idl-iiopnet \
 ..\..\idl\delegate.idl
    cd ..\..\..\..
    cd demo\hello\lib\generated
    $(IDLCOMPILER) -asmVersion $(VERSION) Openbus.Demo.Hello.Idl-iiopnet \
 ..\..\idl\hello.idl
    cd ..\..\..\..


build-base:
    $(BUILD) Openbus.sln

build-examples:
    cd demo\hello
    $(BUILD) DemoHello.sln
    cd ..
    cd delegate
    $(BUILD) DemoDelegate.sln
    cd ..\..


run-tests:
    cd Test\bin\Release
    $(TEST) /testmetadata:..\..\..\Openbus.vsmdi /testlist:FastTests 
    $(TEST) /testmetadata:..\..\..\Openbus.vsmdi /testlist:SlowTests 
    cd ..\..\..


clean-idl:
    cd lib\generated
    rm Openbus.Idl.dll
    cd ..\..

clean-idl-example:
    cd demo\delegate
    rm lib\generated\*.dll
    cd ..
    cd hello
    rm lib\generated\*.dll
    cd ..\..

clean-base:
    $(CLEAN) OpenbusAPI.sln

clean-example:
    cd demo\delegate
    $(CLEAN) DemoDelegate.sln
    cd ..
    cd hello
    $(CLEAN) DemoHello.sln
    cd ..\..


dist:
    if exist package rm -r package
    mkdir package
    mkdir package\doc
    cp doc\Scs.XML package\doc
    xcopy demo\* package\demo /e /i /q
    cp lib\*.dll lib\Resources\*.xsd lib\generated\*.dll package
    zip -r scs.zip package\*

