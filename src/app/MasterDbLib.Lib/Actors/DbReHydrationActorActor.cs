using System;
using Akka.Actor;
using MasterDbLib.Lib.Actors.Messages;
using MasterDbLib.Lib.Caching;

namespace MasterDbLib.Lib.Actors
{
    public class DbReHydrationActorActor : ReceiveActor
    {
        public DbReHydrationActorActor()
        {
            Receive<CheckReHyDrationMessage>(m =>
            {
                try
                {
                    AppDataBase.DbActorFacts[m.ClassName].ReHydrateData();
                }
                catch (Exception e)
                {
                    // log
                }
            });
        }
    }
}