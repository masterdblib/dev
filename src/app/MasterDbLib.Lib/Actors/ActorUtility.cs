using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using MasterDbLib.Lib.Actors.Messages;

namespace MasterDbLib.Lib.Actors.Utility
{
    public class ActorUtility
    {
        public async Task<bool> PerformOperation(IActorRef Sender, DbActorRequestMessage message)
        {
            try
            {
                Sender.Tell(new DbActorResponseMessage(await message.Operation()));
            }
           
            catch (Exception e)
            {
                Sender.Tell(new DbActorResponseMessage(false, e.Message, e));
            }

            return true;
        }
    }
}