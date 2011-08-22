MSBUILD=msbuild /nologo
BUILD=$(MSBUILD) /p:Configuration=Release /verbosity:minimal
CLEAN=$(MSBUILD) /p:Configuration=Release /verbosity:minimal /target:Clean
CLEAND=$(MSBUILD) /p:Configuration=Debug /verbosity:minimal /target:Clean

build: build-base build-examples
test: build run-tests
clean: clean-base clean-example
rebuild: clean build
dist: clean build-base run-dist

build-base:
    $(BUILD) OpenbusAPI.sln

build-examples:
    cd demo\hello
    $(BUILD) DemoHello.sln
    cd ..
    cd delegate
    $(BUILD) DemoDelegate.sln
    cd ..\..

run-tests:
    test\Test_API\bin\Release\Test_API.exe

clean-base:
    $(CLEAN) OpenbusAPI.sln

clean-example:
    cd demo\hello
    $(CLEAN) DemoHello.sln
    $(CLEAND) DemoHello.sln
    cd ..
    cd delegate
    $(CLEAN) DemoDelegate.sln
    $(CLEAND) DemoDelegate.sln
    cd ..\..

run-dist:
    if exist package rd /S /Q package
    mkdir package
    mkdir package\doc
    mkdir package\schema
    copy doc\Scs.XML package\doc
    copy doc\Openbus.XML package\doc
    xcopy demo\* package\demo /e /i /q
    copy lib\SCS*.dll  package
    copy lib\Openbus*.dll  package
    copy lib\generated\*.dll package
    copy lib\log4net.dll package
    copy lib\IIOPChannel.dll package
    copy lib\Resources\*.xsd package\schema
    zip -r scs.zip package -q
    rd /S /Q package
