using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using MasterDbLib.Lib.Actors.Messages;
using MasterDbLib.Lib.Caching;
using MasterDbLib.Lib.DataBaseServices;
using MasterDbLib.Lib.Messages;
using MasterDbLib.Lib.Utility;

namespace MasterDbLib.Lib.Actors
{
    using Akka.Actor;

    public interface DbActorFactory
    {
         Task<bool> ReHydrateData();
    }
    public class DbActorFactory<T>: DbActorFactory where T : IDbEntity, new()
    {
        private static IDb Db { set; get; }
        private static ActorSystem ActorSystem = ActorSystem.Create(Guid.NewGuid().ToString().Replace("-", ""));
        public static IActorRef Writes = ActorSystem.ActorOf(Props.Create(() => new DbActor()), Guid.NewGuid().ToString().Replace("-", ""));
        public static object padlock = new object();
      
        public DbActorFactory(IDb db)
        {
            Db = db;
            if (!AppDataBase.DataExists(typeof(T).Name))
            {
                lock (padlock)
                {
                    if (!AppDataBase.DataExists(typeof(T).Name))
                    {
                        ReHydrateData().Wait(TimeSpan.FromSeconds(60*5));
                    }
                }
            }

            AppDataBase.DbActorFacts.GetOrAdd(typeof(T).Name, this);
        }

        public async Task<bool> ReHydrateData()
        {
            var tmp = LoadAll();
            var live = new ConcurrentDictionary<string,bool>();
           
            foreach (var x in tmp)
            {
                live.GetOrAdd(x.Id, false);
            }


            Parallel.ForEach(await Db.LoadAll<T>().ConfigureAwait(false), (data) =>
            {
                live.GetOrAdd(data.Id, true);
                live[data.Id] = true;
                var result = AppDataBase.SetDataObjectById(typeof(T).Name, new Utility.ActorIdentity(data.Id, data, data.Etag));
            });
            foreach (var keyValuePair in live.Where(x=>!x.Value))
            {
                AppDataBase.DeleteObjectById(typeof(T).Name,   keyValuePair.Key);
            }

            return true;
        }

        public bool IsHydratedInitialized { set; get; }

        public T GetById(string id)
        {
            return DbEntityHelper.Clone(GetByIdFaster(id));
        }

        /// <summary>
        /// Risk of unintended update in memory by reference
        /// </summary>
        /// <returns></returns>
        public T GetByIdFaster(string id)
        {
            var result = AppDataBase.GetDataObjectById(typeof(T).Name, id);
            return ToDbData(id, result).Data;
        }

        public IEnumerable<T> LoadAll()
        {
            var result = AppDataBase.GetDataObjects(typeof(T).Name);
            return result.Select(x => DbEntityHelper.Clone(ToDbData(x.Id, x).Data));
        }

        /// <summary>
        /// Risk of unintended update in memory by reference
        /// </summary>
        /// <returns></returns>
        public IEnumerable<T> LoadAllFaster()
        {
            var result = AppDataBase.GetDataObjects(typeof(T).Name);
            return result.Select(x => ToDbData(x.Id, x).Data);
        }

        public async Task<string> CreateNew(T data)
        {
            var id = await Db.CreateNew(data).ConfigureAwait(false);
            var newd = await Db.GetById<T>(id).ConfigureAwait(false);
            var result = AppDataBase.SetDataObjectById(typeof(T).Name, new Utility.ActorIdentity(id, DbEntityHelper.Clone(newd), newd.Etag));
            return id;
        }

        /// <summary>
        /// Risk of unintended update in memory by reference
        /// </summary>
        /// <returns></returns>
        public async Task<string> CreateNewFaster(T data)
        {
            var id = await Db.CreateNew(data).ConfigureAwait(false);
            var newd = await Db.GetById<T>(id).ConfigureAwait(false);
            var result = AppDataBase.SetDataObjectById(typeof(T).Name, new Utility.ActorIdentity(id, newd, newd.Etag));
            return id;
        }

        public async Task<DbActorResponseMessage> Update(T d, List<string> transactionIds = null)
        {
            Func<Task<bool>> action = async () =>
            {
                var data = AppDataBase.GetDataObjectById(typeof(T).Name, d.Id);
                if (data != null)
                {
                    if (data.Etag == d.Etag)
                    {
                       await Db.Update(d).ConfigureAwait(false);
                        var dat =await Db.GetById<T>(d.Id).ConfigureAwait(false);
                        AppDataBase.SetDataObjectById(typeof(T).Name, new Utility.ActorIdentity(dat.Id, DbEntityHelper.Clone(dat), dat.Etag));
                        return true;
                    }
                }

                return false;
            };
            transactionIds = transactionIds ?? new List<string>();
            transactionIds.Add(d.Id);
            var t = await Writes.Ask(new DbActorRequestMessage(transactionIds.Distinct().ToList(), action), TimeSpan.FromSeconds(500000)).ConfigureAwait(false);

            return t as DbActorResponseMessage;
        }

        /// <summary>
        /// Risk of unintended update in memory by reference
        /// </summary>
        /// <returns></returns>
        public async Task<DbActorResponseMessage> UpdateFaster(T d, List<string> transactionIds = null)
        {
            Func<Task<bool>> action = async () =>
            {
                var data = AppDataBase.GetDataObjectById(typeof(T).Name, d.Id);
                if (data != null)
                {
                    if (data.Etag == d.Etag)
                    {
                       await Db.Update(d).ConfigureAwait(false);
                        var dat =await Db.GetById<T>(d.Id).ConfigureAwait(false);
                        AppDataBase.SetDataObjectById(typeof(T).Name, new Utility.ActorIdentity(dat.Id, dat, dat.Etag));
                    }
                }
                else
                {
                    return false;
                }

                return true;
            };
            transactionIds = transactionIds ?? new List<string>();
            transactionIds.Add(d.Id);
            var t = await Writes.Ask(new DbActorRequestMessage(transactionIds.Distinct().ToList(), action), TimeSpan.FromSeconds(500000)).ConfigureAwait(false);

            return t as DbActorResponseMessage;
        }

        public async Task<DbActorResponseMessage> Delete(T d)
        {
            Func<Task<bool>> action = async () =>
            {
                var data = AppDataBase.GetDataObjectById(typeof(T).Name, d.Id);
                if (data != null)
                {
                    if (data.Etag == d.Etag)
                    {
                       await Db.DeleteById<T>(d.Id).ConfigureAwait(false);
                        AppDataBase.DeleteObjectById(typeof(T).Name, d.Id);
                    }
                }
                else
                {
                    return false;
                }

                return true;
            };
            var t = await Writes.Ask(new DbActorRequestMessage(new List<string>() { d.Id }, action), TimeSpan.FromSeconds(500000)).ConfigureAwait(false);

            return t as DbActorResponseMessage;
        }

        private static DbData<T> ToDbData(string id, Utility.ActorIdentity result)
        {
            return new DbData<T>(id, (T)result?.Data , result?.Etag);
        }
    }
}