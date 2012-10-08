using System;
using audit;
using demo.Properties;
using tecgraf.openbus;

namespace demo {
  /// <summary>
  /// Implementação do servant Messenger.
  /// </summary>  
  public class MessengerImpl : MarshalByRefObject, Messenger {
    private readonly string _entity;

    public MessengerImpl(string entity) {
      _entity = entity;
    }

    #region Messenger Members

    public void showMessage(string message) {
      CallerChain chain = ORBInitializer.Context.CallerChain;
      if (!chain.Caller.entity.Equals(_entity)) {
        Console.WriteLine(Resources.CallChainRefuseMessage +
                          ChainToString.ToString(chain));
        throw new Unauthorized();
      }
      Console.WriteLine(Resources.CallChainAcceptMessage +
                        ChainToString.ToString(chain) + ": " + message);
    }

    #endregion
  }
}