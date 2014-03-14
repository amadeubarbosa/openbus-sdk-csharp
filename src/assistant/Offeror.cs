using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using log4net;
using omg.org.CORBA;
using scs.core;
using tecgraf.openbus.core.v2_0.services.access_control;
using tecgraf.openbus.core.v2_0.services.offer_registry;
using tecgraf.openbus.lease;

namespace tecgraf.openbus.assistant {
  internal class Offeror {
    private readonly AssistantImpl _assistant;

    #region Fields

    private static readonly ILog Logger =
      LogManager.GetLogger(typeof(RenewerTask));

    private readonly Thread _registeringThread;

    private readonly AutoResetEvent _autoEvent = new AutoResetEvent(false);
    private readonly ConcurrentDictionary<IComponent, IList<Offer>> _offers;
    private static volatile bool _shutdown;

    #endregion

    public Offeror(AssistantImpl assistant) {
      _assistant = assistant;
      _offers = new ConcurrentDictionary<IComponent, IList<Offer>>();
      _registeringThread = new Thread(Run) { Name = "AssistantOfferor", IsBackground = true };
      _registeringThread.Start();
    }

    private void Run() {
      try {
        // loop para aguardar notificações de que deve avaliar os registros
        while (_autoEvent.WaitOne()) {
          if (_shutdown) {
            break;
          }
          // loop para tentar novamente se alguma oferta der erro no registro. Cada oferta a ser registrada só executa uma vez dentro desse loop. Assim, se uma der repetidamente erro de not authorized, por exemplo, isso não impedirá que outras sejam registradas normalmente.
          bool tryAgain = false;
          do {
            // loop para avaliar todas as ofertas e ver quais precisam ser registradas novamente
            foreach (Offer offer in _offers.SelectMany(pair => pair.Value)) {
              // só altera o tryAgain uma vez
              if (!offer.Register() && !tryAgain) {
                tryAgain = true;
              }
            }
            if (tryAgain) {
              Thread.Sleep(_assistant.Properties.IntervalMillis);
            }
          } while (tryAgain);
        }
        Logger.Debug("Thread de registro de ofertas do assistente finalizada.");
      }
      catch (ThreadInterruptedException) {
        Logger.Warn("Registro de ofertas do assistente interrompido.");
      }
    }

    public void Activate() {
      _autoEvent.Set();
    }

    public void Finish() {
      _shutdown = true;
      Activate();
      // Não devo dar join "em mim mesmo" para evitar deadlock
      // Comentei pois o assistente dará logout e a thread de registro é de background, então não precisa esperar tudo finalizar ok já que o barramento removerá as ofertas automaticamente.
//      if (!Thread.CurrentThread.ManagedThreadId.Equals(_registeringThread.ManagedThreadId)) {
//        _registeringThread.Join();
//      }
    }

    public void AddOffer(IComponent ic, ServiceProperty[] properties) {
      if (!_offers.ContainsKey(ic)) {
        _offers.TryAdd(ic, new List<Offer>());
      }
      _offers[ic].Add(new Offer(_assistant, ic, properties));
    }

    public void RemoveOffers(IComponent ic) {
      if (_offers.ContainsKey(ic)) {
        // remove do conjunto
        IList<Offer> removed;
        _offers.TryRemove(ic, out removed);
        // cancela todas as ofertas caso alguma esteja com registro em andamento
        foreach (Offer offer in removed) {
          offer.Cancel();
        }
      }
    }

    public void RemoveAllOffers() {
      foreach (KeyValuePair<IComponent, IList<Offer>> pair in _offers) {
        RemoveOffers(pair.Key);
      }
    }

    public void ResetAllOffers() {
      foreach (Offer offer in _offers.SelectMany(pair => _offers[pair.Key])) {
        offer.Reset();
      }
    }

    private class Offer {
      private readonly AssistantImpl _assistant;
      private readonly IComponent _ic;
      private readonly ServiceProperty[] _properties;
      private ServiceOffer _registeredOffer;
      private volatile bool _canceled;
      private volatile bool _active;
      private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

      public Offer(AssistantImpl assistant, IComponent ic,
                      ServiceProperty[] properties) {
        _assistant = assistant;
        _ic = ic;
        _properties = properties;
      }

      public void Reset() {
        bool succeeded = false;
        while (!succeeded) {
          _lock.EnterWriteLock();
          try {
            if (!_active) {
              _registeredOffer = null;
              _canceled = false;
              succeeded = true;
            }
          }
          finally {
            _lock.ExitWriteLock();
          }
          // força a troca de contexto
          Thread.Sleep(1);
        }
      }

      public bool Register() {
        if (_shutdown) {
          return true;
        }
        bool register = false;
        _lock.EnterReadLock();
        try {
          if (!_active && !_canceled && _registeredOffer == null) {
            register = _active = true;
          }
        }
        finally {
          _lock.ExitReadLock();
        }
        if (!register) {
          return true;
        }
        Exception caught = null;
        bool succeeded = false;
        try {
          // Registra a oferta no barramento
          ServiceOffer offeredService =
            ORBInitializer.Context.OfferRegistry.registerService(_ic,
                                                                 _properties);
          bool canceled;
          ServiceOffer selfOffer;
          _lock.EnterWriteLock();
          try {
            canceled = _canceled;
            selfOffer = _registeredOffer;
          }
          finally {
            _lock.ExitWriteLock();
          }
          // nesse ponto, se o login for perdido, a oferta será aceita aqui mas será automaticamente removida do barramento. No entanto, o offeror avaliará novamente essa oferta e verá que precisa ser registrada novamente.
          if (selfOffer == null && !canceled) {
            _lock.EnterWriteLock();
            try {
              _registeredOffer = offeredService;
              // libera a oferta
              _active = false;
            }
            finally {
              _lock.ExitWriteLock();
            }
          }
          else {
            // registro da oferta foi cancelado
            if (canceled) {
              Logger.Info(
                "O registro dessa oferta foi cancelado, mas a oferta já havia sido registrada. Removendo a oferta.");
              RemoveOfferFromBus();
              Logger.Info("Oferta removida após cancelamento.");
            }
            // libera a oferta
            _lock.EnterWriteLock();
            try {
              _active = false;
            }
            finally {
              _lock.ExitWriteLock();
            }
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
            if (_assistant.Properties.RegisterFailureCallback != null) {
              _assistant.Properties.RegisterFailureCallback(_assistant, _ic,
                                                            _properties, caught);
            }
          }
          catch (Exception e) {
            Logger.Error(
              "Erro ao executar a callback de falha de registro fornecida pelo usuário.",
              e);
          }
        }
        return succeeded;
      }

      public void Cancel() {
        _lock.EnterWriteLock();
        try {
          if (_registeredOffer != null && !_canceled) {
            Logger.Info(
              "Pedido de cancelamento de oferta recebido. Oferta existente será removida.");
            _canceled = true;
            Logger.Info("Removendo oferta existente.");
            new Thread(RemoveOfferFromBus) { IsBackground = true }.Start();
            return;
          }
          Logger.Info(
            "Pedido de cancelamento de oferta recebido. A oferta não será registrada até que um reset seja feito. Se um registro estiver em andamento, a oferta recém-registrada será removida.");
          _canceled = true;
        }
        finally {
          _lock.EnterWriteLock();
        }
      }

      private void RemoveOfferFromBus() {
        bool succeeded = false;
        Exception caught = null;
        while (!succeeded) {
          try {
            _registeredOffer.remove();
            succeeded = true;
            _registeredOffer = null;
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
              if (_assistant.Properties.RemoveOfferFailureCallback != null) {
                _assistant.Properties.RemoveOfferFailureCallback(_assistant, _ic,
                                                                 _properties,
                                                                 caught);
              }
            }
            catch (Exception e) {
              Logger.Error(
                "Erro ao executar a callback de falha de registro fornecida pelo usuário.",
                e);
            }
            Thread.Sleep(_assistant.Properties.IntervalMillis);
          }
        }
      }
    }
  }
}