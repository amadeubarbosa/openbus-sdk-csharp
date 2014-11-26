using System;
using System.Reflection;
using System.Text;
using System.Threading;
using Ch.Elca.Iiop.Idl;
using demo.Properties;
using omg.org.CORBA;
using tecgraf.openbus.assistant;
using tecgraf.openbus.core.v2_0.services.access_control;
using tecgraf.openbus.core.v2_0.services.offer_registry;

namespace demo {
  /// <summary>
  /// Cliente da demo Independent Clock.
  /// </summary>
  internal static class IndependentClockClient {
    private static void Main(String[] args) {
      // Obtém dados através dos argumentos
      string host = args[0];
      ushort port = Convert.ToUInt16(args[1]);
      string entity = args[2];
      byte[] password = new ASCIIEncoding().GetBytes(args[3]);
      int interval = Convert.ToInt32(args.Length > 4 ? args[4] : "1");

      // Usa o assistente do OpenBus para se conectar ao barramento e realizar a autenticação.
      AssistantProperties props = new PasswordProperties(entity, password) {
        Interval = interval,
        LoginFailureCallback = LoginFailureCallback,
        FindFailureCallback = FindFailureCallback
      };
      Assistant assistant = new AssistantImpl(host, port, props);

      // Cria o finder que será responsável por encontrar o servidor no barramento
      Finder finder = new Finder(assistant, interval);

      // Imprime a hora, buscando a faceta clock em outra thread se necessário
      while (true) {
        bool failed = true;
        Clock clock = finder.Clock;
        if (clock != null) {
          try {
            // utiliza o serviço
            long ticks = clock.getTimeInTicks();
            Console.WriteLine("Hora do servidor: {0:HH:mm:ss}",
              new DateTime(ticks));
            failed = false;
          }
          catch (TRANSIENT) {
            Console.WriteLine(Resources.ServiceTransientErrorMsg);
          }
          catch (COMM_FAILURE) {
            Console.WriteLine(Resources.ServiceCommFailureErrorMsg);
          }
          catch (Exception e) {
            NO_PERMISSION npe = null;
            if (e is TargetInvocationException) {
              // caso seja uma exceção lançada pelo SDK, será uma NO_PERMISSION
              npe = e.InnerException as NO_PERMISSION;
            }
            if ((npe == null) && (!(e is NO_PERMISSION))) {
              // caso não seja uma NO_PERMISSION não é uma exceção esperada então deixamos passar.
              throw;
            }
            npe = npe ?? e as NO_PERMISSION;
            bool found = false;
            string message = String.Empty;
            switch (npe.Minor) {
              case NoLoginCode.ConstVal:
                message = Resources.NoLoginCodeErrorMsg;
                found = true;
                break;
              case UnknownBusCode.ConstVal:
                message = Resources.UnknownBusCodeErrorMsg;
                found = true;
                break;
              case UnverifiedLoginCode.ConstVal:
                message = Resources.UnverifiedLoginCodeErrorMsg;
                found = true;
                break;
              case InvalidRemoteCode.ConstVal:
                message = Resources.InvalidRemoteCodeErrorMsg;
                found = true;
                break;
            }
            if (found) {
              Console.WriteLine(message);
            }
            else {
              throw;
            }
          }
          if (failed) {
            // descarta o clock atual e busca um novo
            finder.Clock = null;
          }
        }
        if (failed) {
          finder.Activate();
          Console.WriteLine("Hora local: {0:HH:mm:ss}", DateTime.Now);
        }
        Thread.Sleep(interval * 1000);
      }
    }

    private static void FindFailureCallback(Assistant assistant, Exception e) {
      Console.WriteLine(Resources.FindFailureCallback + e);
    }

    private static void LoginFailureCallback(Assistant assistant, Exception e) {
      Console.WriteLine(Resources.LoginFailureCallback + e);
    }
  }

  internal class Finder {
    private Clock _clock;
    private readonly Assistant _assistant;
    private readonly int _interval;
    private bool _active;
    // lock propositalmente não é um ReaderWriterLockSlim pois não queremos otimizar as leituras
    private readonly object _lock;
    private readonly ServiceProperty[] _properties;

    internal Finder(Assistant assistant, int interval) {
      _assistant = assistant;
      _interval = interval;
      _clock = null;
      _active = false;
      _lock = new object();
      // propriedade gerada automaticamente
      ServiceProperty autoProp =
        new ServiceProperty("openbus.component.interface",
                            Repository.GetRepositoryID(typeof (Clock)));
      // propriedade definida pelo serviço independent clock
      ServiceProperty prop = new ServiceProperty("offer.domain",
                                                 "Demo Independent Clock");
      _properties = new[] {prop, autoProp};
    }

    internal Clock Clock {
      get {
        lock (_lock) {
          return _clock;
        }
      }
      set {
        lock (_lock) {
          _clock = value;
        }
      }
    }

    internal void Activate() {
      bool find = false;
      lock (_lock) {
        if (_clock == null && !_active) {
          find = _active = true;
        }
      }
      if (find) {
        // faz a busca em outra thread para não travar essa
        ThreadStart ts = Find;
        Thread finderThread = new Thread(ts) { IsBackground = true };
        finderThread.Start();
      }
    }

    private void Find() {
      try {
        // Faz busca utilizando propriedades geradas automaticamente e propriedades definidas pelo serviço específico
        while (true) {
          ServiceOfferDesc[] offers = Utils.FilterWorkingOffers(_assistant.FindServices(_properties, -1));
          // analiza as ofertas encontradas
          bool foundClock = false;
          if (offers != null) {
            if (offers.Length < 1) {
              Console.WriteLine(Resources.ServiceNotFound);
            }
            else {
              if (offers.Length > 1) {
                Console.WriteLine(Resources.ServiceFoundMoreThanExpected);
              }
              foreach (ServiceOfferDesc serviceOfferDesc in offers) {
                Console.WriteLine(Resources.ServiceFoundTesting);
                try {
                  MarshalByRefObject clockObj =
                    serviceOfferDesc.service_ref.getFacet(
                      Repository.GetRepositoryID(typeof(Clock)));
                  if (clockObj == null) {
                    Console.WriteLine(Resources.FacetNotFoundInOffer);
                    continue;
                  }
                  Clock clock = clockObj as Clock;
                  if (clock == null) {
                    Console.WriteLine(Resources.FacetFoundWrongType);
                    continue;
                  }
                  Console.WriteLine(Resources.OfferFound);
                  // achou o serviço
                  lock (_lock) {
                    _clock = clock;
                    _active = false;
                    foundClock = true;
                  }
                  break;
                }
                catch (TRANSIENT) {
                  Console.WriteLine(Resources.ServiceTransientErrorMsg);
                }
                catch (COMM_FAILURE) {
                  Console.WriteLine(Resources.ServiceCommFailureErrorMsg);
                }
                catch (Exception e) {
                  NO_PERMISSION npe = null;
                  if (e is TargetInvocationException) {
                    // caso seja uma exceção lançada pelo SDK, será uma NO_PERMISSION
                    npe = e.InnerException as NO_PERMISSION;
                  }
                  if ((npe == null) && (!(e is NO_PERMISSION))) {
                    // caso não seja uma NO_PERMISSION não é uma exceção esperada então deixamos passar.
                    throw;
                  }
                  npe = npe ?? e as NO_PERMISSION;
                  bool found = false;
                  string message = String.Empty;
                  switch (npe.Minor) {
                    case NoLoginCode.ConstVal:
                      message = Resources.NoLoginCodeErrorMsg;
                      found = true;
                      break;
                    case UnknownBusCode.ConstVal:
                      message = Resources.UnknownBusCodeErrorMsg;
                      found = true;
                      break;
                    case UnverifiedLoginCode.ConstVal:
                      message = Resources.UnverifiedLoginCodeErrorMsg;
                      found = true;
                      break;
                    case InvalidRemoteCode.ConstVal:
                      message = Resources.InvalidRemoteCodeErrorMsg;
                      found = true;
                      break;
                  }
                  if (found) {
                    Console.WriteLine(message);
                  }
                  else {
                    throw;
                  }
                }
              }
              if (foundClock) {
                break;
              }
              Console.WriteLine(Resources.OfferFunctionalNotFound);
            }
          }
          Thread.Sleep(_interval * 1000);
        }
      }
      catch (ThreadInterruptedException) {
        // não faz nada, thread principal abortou essa thread
      }
    }
  }
}