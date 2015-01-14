using System;
using System.Collections.Generic;
using Ch.Elca.Iiop.Idl;
using omg.org.CORBA;
using tecgraf.openbus.assistant;
using tecgraf.openbus.core.v2_0.services.offer_registry;
using tecgraf.openbus.exceptions;
using tecgraf.openbus.interop.simple;

namespace tecgraf.openbus.interop.chaining {
  /// <summary>
  ///   Implementação do servant HelloProxy.
  /// </summary>
  public class ProxyImpl : MarshalByRefObject, HelloProxy {
    #region HelloProxy Members

    public string fetchHello(byte[] encodedChain) {
      OpenBusContext context = ORBInitializer.Context;
      CallerChain chain;
      try {
        chain = context.DecodeChain(encodedChain);
      }
      catch (InvalidEncodedStreamException e) {
        Console.WriteLine(e);
        throw new ArgumentException("Cadeia em formato inválido.", e);
      }
      context.JoinChain(chain);

      ServiceProperty[] properties = {
        new ServiceProperty("offer.domain", "Interoperability Tests"),
        new ServiceProperty("openbus.component.interface",
          Repository.GetRepositoryID(typeof (Hello))),
        new ServiceProperty("openbus.component.name", "RestrictedHello")
      };
      List<ServiceOfferDesc> offers =
        Utils.FindOffer(ORBInitializer.Context.OfferRegistry, properties, 1, 10, 1);

      foreach (ServiceOfferDesc desc in offers) {
        try {
          if (OrbServices.GetSingleton().non_existent(desc.service_ref)) {
            continue;
          }
        }
        catch (TRANSIENT) {
          continue;
        }
        catch (COMM_FAILURE) {
          continue;
        }
        MarshalByRefObject helloObj =
          desc.service_ref.getFacet(Repository.GetRepositoryID(typeof (Hello)));
        if (helloObj == null) {
          Console.WriteLine(
            "Não foi possível encontrar uma faceta com esse nome.");
          continue;
        }

        Hello hello = helloObj as Hello;
        if (hello == null) {
          Console.WriteLine("Faceta encontrada não implementa Hello.");
          continue;
        }
        return hello.sayHello();
      }

      return "";
    }

    #endregion

    public override object InitializeLifetimeService() {
      return null;
    }
  }
}