using System;
using Ch.Elca.Iiop.CorbaObjRef;
using omg.org.CORBA;
using omg.org.IOP;
using omg.org.PortableInterceptor;
using OpenbusAPI.Logger;

namespace OpenbusAPI.Interceptors
{
  class FTClientInterceptor : ClientInterceptor
  {

    #region Contructor

    /// <summary>
    /// Inicializa uma nova instância de OpenbusAPI.Interceptors.ClientInterceptor
    /// </summary>
    /// <param name="codec">Codificador</param>
    public FTClientInterceptor(Codec codec)
      : base(codec) {
    }

    #endregion

    #region ClientRequestInterceptor Members

    public override void receive_exception(ClientRequestInfo ri) {

      bool fetch =
        ri.received_exception_id.Equals("IDL:omg.org/CORBA/TRANSIENT:1.0") ||
        ri.received_exception_id.Equals("IDL:omg.org/CORBA/OBJECT_NOT_EXIST:1.0") ||
        ri.received_exception_id.Equals("IDL:omg.org/CORBA/COMM_FAILURE:1.0");

      if (!fetch) {
        Log.INTERCEPTORS.Fatal(ri.received_exception_id);
        return;
      }

      Openbus openbus = Openbus.GetInstance();
      FaultToleranceManager ftManager = openbus.GetFaultToleranceManager();
      String key = getObjectKey(ri);

      Log.INTERCEPTORS.Fatal(key);

      bool acsError =
      key.Equals(Openbus.OPENBUS_KEY) ||
      key.Equals(Openbus.ACCESS_CONTROL_SERVICE_KEY) ||
      key.Equals(Openbus.LEASE_PROVIDER_KEY) ||
      key.Equals(Openbus.FAULT_TOLERANT_ACS_KEY);

      if (acsError) {
        bool ok = ftManager.UpdateOpenbus(openbus);
        if (!ok) {
          Log.INTERCEPTORS.Fatal("Não foi possível se reconectar ao barramento");
          return;
        }

        if (key.Equals(Openbus.ACCESS_CONTROL_SERVICE_KEY)) {
          throw new ForwardRequest(openbus.GetAccessControlService()
              as MarshalByRefObject);
        }
        else if (key.Equals(Openbus.LEASE_PROVIDER_KEY)) {
          throw new ForwardRequest(openbus.GetLeaseProvider()
              as MarshalByRefObject);
        }
        else if (key.Equals(Openbus.OPENBUS_KEY)) {
          throw new ForwardRequest(openbus.GetACSComponent()
              as MarshalByRefObject);
        }
      }
      else if (key.Equals(Openbus.REGISTRY_SERVICE_KEY)) {
        throw new ForwardRequest(openbus.GetRegistryService()
            as MarshalByRefObject);
      }
    }

    private string getObjectKey(ClientRequestInfo ri) {
      ORB orb = omg.org.CORBA.OrbServices.GetSingleton();
      String objString = orb.object_to_string(ri.target);
      Ior ior = new Ior(objString);
      byte[] objKey = ior.FindInternetIiopProfile().ObjectKey;

      System.Text.Encoding enc = System.Text.Encoding.ASCII;
      return enc.GetString(objKey);
    }

    #endregion

  }
}