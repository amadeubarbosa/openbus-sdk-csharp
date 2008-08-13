PROJNAME= DASTester
APPNAME= das

OPENBUSINC = ${OPENBUS_HOME}/incpath
OPENBUSLIB = ${OPENBUS_HOME}/libpath/${TEC_UNAME}

INCLUDES= ${OPENBUS_HOME}/core/utilities/cppoil ${OPENBUSINC}/cxxtest ${OPENBUSINC}/tolua5.1 ${OPENBUSINC}/scs
LDIR= ${OPENBUSLIB}

LIBS= dl 

SLIB= ${OPENBUS_HOME}/core/utilities/cppoil/lib/${TEC_UNAME}/libopenbus.a \
      ${OPENBUSLIB}/libscsoil.a \
      ${OPENBUSLIB}/liboilall.a \
      ${OPENBUSLIB}/libluasocket.a \
      ${OPENBUSLIB}/libtolua5.1.a

SRC= runner.cpp 

USE_LUA51=YES
USE_STATIC=YES
NO_SCRIPTS=YES

cxxtest:
	cxxtestgen.pl --runner=StdioPrinter -o runner.cpp DASTestSuite.cpp
