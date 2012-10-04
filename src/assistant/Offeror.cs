using System;
using System.Threading;
using log4net;
using omg.org.CORBA;
using scs.core;
using tecgraf.openbus.core.v2_0.services.access_control;
using tecgraf.openbus.core.v2_0.services.offer_registry;

namespace tecgraf.openbus.assistant {
  internal class Offeror {
    private static readonly ILog Logger = LogManager.GetLogger(typeof(Offeror));

    private readonly IComponent _ic;
    private readonly AssistantImpl _assistant;
    private bool _active;
    // lock propositalmente não é um ReaderWriterLockSlim pois não queremos otimizar as leituras
    private readonly object _lock;
    private ServiceOffer _offer;
    private bool _canceled;

    internal Offeror(IComponent component, ServiceProperty[] properties, AssistantImpl assistant) {
      _ic = component;
      _assistant = assistant;
      Properties = properties;
      _lock = new object();
      _active = false;
      _canceled = false;
    }

    internal ServiceProperty[] Properties { get; private set; }

    internal void Reset() {
      lock (_lock) {
        _offer = null;
        _canceled = false;
      }
    }

    internal void Cancel() {
      ServiceOffer offer;
      bool canceled;
      lock (_lock) {
        offer = _offer;
        canceled = _canceled;
      }
      if (offer != null && !canceled) {
        Logger.Info("Pedido de cancelamento de oferta recebido. Oferta existente será removida.");
        lock (_lock) {
          _canceled = true;
        }
        Logger.Info("Removendo oferta existente.");
        //TODO fazer remove sincronamente? se deixar sincrono o shutdown do assistente pode nao retornar nunca. se deixar assincrono o shutdown pode terminar antes e sem outras threads, as threads de background morrerão. As ofertas que não tiverem sido removidas só o serão quando o login for invalidado no barramento.
        new Thread(() => RemoveOffer(offer)) { IsBackground = true }.Start();
        //RemoveOffer(offer);
        return;
      }
      Logger.Info("Pedido de cancelamento de oferta recebido. A oferta não será registrada até que um reset seja feito. Se um registro estiver em andamento, a oferta recém-registrada será removida.");
      lock (_lock) {
        _canceled = true;
      }
    }

    internal void Activate() {
      bool register = false;
      lock (_lock) {
        if (!_active && _offer == null && !_canceled) {
          register = _active = true;
        }
      }
      if (register) {
        new Thread(Register) {IsBackground = true}.Start();
      }
    }

    private void Register() {
      bool succeeded = false;
      Exception caught = null;
      while (!succeeded) {
        try {
          // Registra a oferta no barramento
          ServiceOffer offer =
            ORBInitializer.Context.OfferRegistry.registerService(_ic,
                                                                 Properties);
          bool canceled;
          ServiceOffer selfOffer;
          lock (_lock) {
            canceled = _canceled;
            selfOffer = _offer;
          }
          if (selfOffer == null && !canceled) {
            lock (_lock) {
              _offer = offer;
            }
          }
          else {
            // registro da oferta foi cancelado
            if (canceled) {
              Logger.Info("O registro dessa oferta foi cancelado, mas a oferta já havia sido registrada. Removendo a oferta.");
              RemoveOffer(offer);
              Logger.Info("Oferta removida após cancelamento.");
            }
          }
          // libera o offeror para novo uso
          lock (_lock) {
            _active = false;
          }
          succeeded = true;
        }
        catch (NO_PERMISSION e) {
          if (e.Minor == NoLoginCode.ConstVal) {
            caught = e;
          }
          else {
            throw;
          }
        }
        catch (Exception e) {
          caught = e;
        }
        if (!succeeded) {
          try {
            _assistant.Properties.FailureCallback.OnRegisterFailure(_assistant, _ic, Properties, caught);
          }
          catch (Exception e) {
            Logger.Error("Erro ao executar a callback de falha de registro fornecida pelo usuário.", e);
          }
          Thread.Sleep(_assistant.Properties.Interval * 1000);
        }
      }
    }

    private void RemoveOffer(ServiceOffer offer) {
      bool succeeded = false;
      Exception caught = null;
      while (!succeeded) {
        try {
          offer.remove();
          succeeded = true;
        }
        catch (NO_PERMISSION e) {
          if (e.Minor == NoLoginCode.ConstVal) {
            caught = e;
          }
          else {
            throw;
          }
        }
        catch (Exception e) {
          caught = e;
        }
        if (!succeeded) {
          Logger.Error("Erro ao tentar remover uma oferta cancelada.");
          try {
            //todo chamar mesmo a callback aqui? como informar que foi um erro no cancelamento e nao no registro?
            _assistant.Properties.FailureCallback.OnRegisterFailure(_assistant, _ic, Properties, caught);
          }
          catch (Exception e) {
            Logger.Error("Erro ao executar a callback de falha de registro fornecida pelo usuário.", e);
          }
          Thread.Sleep(_assistant.Properties.Interval * 1000);
        }
      }
    }
  }
}