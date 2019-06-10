using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using MasterDbLib.Lib.Actors;
using MasterDbLib.Lib.Actors.Utility;

namespace MasterDbLib.Lib.Caching
{
    public class AppDataBase
    {
        internal static bool TurnOnEventualConsistencyReloading { set; get; }
        internal static TimeSpan EventualConsistencyReloadingInterval = TimeSpan.FromMinutes(1);
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ActorIdentity>> ChildActors =
            new ConcurrentDictionary<string, ConcurrentDictionary<string, ActorIdentity>>();

        private static readonly ConcurrentDictionary<string,DateTime> LastAccessUtc = new ConcurrentDictionary<string, DateTime>();
        public static readonly ConcurrentDictionary<string, DbActorFactory> DbActorFacts = new ConcurrentDictionary<string, DbActorFactory>();
        

        public static bool DataExists(string className)
        {
            return AppDataBase.ChildActors.Count != 0;
        }

        public static DateTime GetLastAccessUtc(string className)
        {
            return LastAccessUtc.GetOrAdd(className, DateTime.UtcNow.AddYears(-1));
        }
        public static List<string> GetAllAccessedClasses()
        {
            return LastAccessUtc.Select(x => x.Key).ToList();
        }
        public static bool HasRecentlyAccessed(string className)
        {
            return (GetLastAccessUtc(className) - DateTime.UtcNow).TotalMinutes <= 5;
        }
        public static void ResetLastAccessUtc(string className)
        {
             GetLastAccessUtc(className);
             LastAccessUtc[className]= DateTime.UtcNow.AddYears(-1);
        }
        public static ActorIdentity GetDataObjectById(string className, string id)
        {
            LastAccessUtc.GetOrAdd(className, DateTime.UtcNow);
            LastAccessUtc[className] = DateTime.UtcNow;
            ChildActors.GetOrAdd(className, new ConcurrentDictionary<string, ActorIdentity>());
            if (ChildActors.ContainsKey(className))
            {
                if (ChildActors[className].ContainsKey(id))
                {
                    return ChildActors[className][id];
                }
            }

            return null;
        }

        public static bool SetDataObjectById(string className, ActorIdentity data)
        {
            LastAccessUtc.GetOrAdd(className, DateTime.UtcNow);
            LastAccessUtc[className] = DateTime.UtcNow;
            ChildActors.GetOrAdd(className, new ConcurrentDictionary<string, ActorIdentity>());
            ChildActors[className].GetOrAdd(data.Id, data);
            ChildActors[className][data.Id] = data;
            return true;
        }

        public static IEnumerable<ActorIdentity> GetDataObjects(string className)
        {
            LastAccessUtc.GetOrAdd(className, DateTime.UtcNow);
            LastAccessUtc[className] = DateTime.UtcNow;
            if (ChildActors.ContainsKey(className))
            {
                return ChildActors[className].Select(x => x.Value);
            }
            return new List<ActorIdentity>();
        }

        public static bool DataHasChangedBasedOnEtag(string className, string id, string etag)
        {
            var actor = GetDataObjectById(className, id);
            return actor != null && actor.Etag != etag;
        }

        internal static void DeleteObjectById(string className, string id)
        {
            LastAccessUtc.GetOrAdd(className, DateTime.UtcNow);
            LastAccessUtc[className] = DateTime.UtcNow;
            var actor = GetDataObjectById(className, id);
            if (actor != null)
            {
                var result = ChildActors[className].TryRemove(id, out actor);
            }
        }
    }
}