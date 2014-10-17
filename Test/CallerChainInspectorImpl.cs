using System;
using System.Collections.Generic;
using tecgraf.openbus.core.v2_1.services.access_control;
using test;

namespace tecgraf.openbus.test {
  public class CallerChainInspectorImpl : MarshalByRefObject, CallerChainInspector {
  /**
   * O Contexto.
   */
  private readonly OpenBusContext _context;

  /**
   * Construtor.
   * 
   * @param context o contexto.
   */
  public CallerChainInspectorImpl() {
    ORBInitializer.InitORB();
    _context = ORBInitializer.Context;
  }

  public string[] listCallers() {
    CallerChain chain = _context.CallerChain;
    IList<String> list = new List<String>();
    foreach (LoginInfo info in chain.Originators) {
      list.Add(info.entity);
    }
    list.Add(chain.Caller.entity);
    string[] array = new string[list.Count];
    list.CopyTo(array, 0);
    return array;
  }

  public string[] listCallerLogins() {
    CallerChain chain = _context.CallerChain;
    IList<String> list = new List<String>();
    foreach (LoginInfo info in chain.Originators) {
      list.Add(info.id);
    }
    list.Add(chain.Caller.id);
    string[] array = new string[list.Count];
    list.CopyTo(array, 0);
    return array;
  }

  }
}
