using Akka.Actor;
using Akka.Util.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using MasterDbLib.Lib.Actors.Messages;
using MasterDbLib.Lib.Caching;

namespace MasterDbLib.Lib.Actors
{
    public class DbActor : ReceiveActor
    {
        private Dictionary<string, IActorRef> _actors = new Dictionary<string, IActorRef>();
        private Dictionary<string, IActorRef> _transactionActors = new Dictionary<string, IActorRef>();
        private Dictionary<string, List<string>> _transactions = new Dictionary<string, List<string>>();

        private IActorRef DbReHydrationActorActor =
            Context.System.ActorOf(Props.Create(() => new DbReHydrationActorActor()));

        public DbActor()
        {
            Context.System.Scheduler.ScheduleTellRepeatedly(AppDataBase.EventualConsistencyReloadingInterval, AppDataBase.EventualConsistencyReloadingInterval, Self, new CheckReHyDrationMessage(), Self);
            Receive<CheckReHyDrationMessage>(_ =>
            {
                if (AppDataBase.TurnOnEventualConsistencyReloading)
                {
                    foreach (var className in AppDataBase.GetAllAccessedClasses())
                    {
                        if (AppDataBase.HasRecentlyAccessed(className))
                        {
                            AppDataBase.ResetLastAccessUtc(className);
                            if (AppDataBase.DbActorFacts.ContainsKey(className))
                            {
                                DbReHydrationActorActor.Tell(new CheckReHyDrationMessage(className));
                            }
                        }
                    }
                }
            });
            this.Receive<DbActorRequestMessage>(message =>
            {
                if (message.TransactionId == null || message.TransactionId.Count == 0)
                {
                    return;
                }

                if (message.Operation == null)
                {
                    RemoveAllStoredTransactionRoutes(message);
                    return;
                }
                string transactionSig = null;
                transactionSig = GetTransactionSigIfTransactionIdIsPresentInTransactionRoute(message);

                if (transactionSig == null && message.TransactionId.Count > 1)
                {
                    transactionSig = CreateNewTransactionActor(transactionSig, message);
                }
                if (transactionSig != null)
                {
                    _transactionActors[transactionSig].Forward(message);
                }
                else
                {
                    var id = message.TransactionId[0];
                    if (!_actors.ContainsKey(id))
                    {
                        _actors.Add(id, Context.System.ActorOf(Props.Create(() => new DbPerIdActor(id, false))));
                    }
                    _actors[id].Forward(message);
                }
            });
        }

        private string CreateNewTransactionActor(string transactionSig, DbActorRequestMessage message)
        {
            transactionSig = Guid.NewGuid().ToString();
            _transactions.Add(transactionSig, message.TransactionId);
            _transactionActors.Add(transactionSig,
                Context.System.ActorOf(Props.Create(() => new DbPerIdActor(transactionSig, true))));
            return transactionSig;
        }

        private string GetTransactionSigIfTransactionIdIsPresentInTransactionRoute(DbActorRequestMessage message)
        {
            string transactionSig = null;
            _transactions.ForEach(x =>
                {
                    if (Enumerable.Count<string>(x.Value, y => message.TransactionId.Count(z => z == y) > 0) > 0)
                    {
                        x.Value.AddRange(message.TransactionId);
                        transactionSig = x.Key;
                    }
                }
            );
            return transactionSig;
        }

        private void RemoveAllStoredTransactionRoutes(DbActorRequestMessage message)
        {
            _transactions.ForEach(x =>
            {
                if (x.Value.Count(y => message.TransactionId.Count(z => z == y) > 0) > 0)
                {
                    foreach (var s in message.TransactionId)
                    {
                        x.Value.Remove(s);
                    }
                }
            });
        }
    }
}