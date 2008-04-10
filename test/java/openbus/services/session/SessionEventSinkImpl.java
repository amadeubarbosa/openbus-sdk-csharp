/*
 * $Id$
 */
package openbus.services.session;

import openbusidl.ss.SessionEvent;
import openbusidl.ss.SessionEventSinkPOA;

/**
 * Implementa��o de um tratador de eventos de sess�o.
 * 
 * @author Tecgraf/PUC-Rio
 */
public final class SessionEventSinkImpl extends SessionEventSinkPOA {
  /**
   * {@inheritDoc}
   */
  public void push(SessionEvent ev) {
    System.out.println("Chegou evento: " + ev.type);
  }

  /**
   * {@inheritDoc}
   */
  public void disconnect() {
    System.out.println("Desconex�o solicitada...");
  }
}