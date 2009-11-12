#!/bin/ksh

if [ -z "${OPENBUS_HOME}" ] ; then
  echo "[ERRO] Vari�vel de ambiente OPENBUS_HOME n�o definida"
  exit 1
fi

exec ${OPENBUS_HOME}/core/bin/servicelauncher ${OPENBUS_HOME}/core/management/management.lua "$@"
