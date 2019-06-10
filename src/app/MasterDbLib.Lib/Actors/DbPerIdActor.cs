using Akka.Actor;
using MasterDbLib.Lib.Actors.Messages;
using MasterDbLib.Lib.Actors.Utility;

namespace MasterDbLib.Lib.Actors
{
    public class DbPerIdActor : ReceiveActor
    {
        public string Id { set; get; }
        private bool IsTransaction { set; get; }

        public DbPerIdActor(string id, bool isTransaction = false)
        {
            IsTransaction = isTransaction;
            Id = id;
            this.ReceiveAsync<DbActorRequestMessage>(async message =>
            {
                await new ActorUtility().PerformOperation(Sender, message);
                Context.Parent.Tell(message.TransactionId);
            });
        }
    }
}