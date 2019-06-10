using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MasterDbLib.Lib.Actors;
using MasterDbLib.Lib.Actors.Messages;
using MasterDbLib.Lib.Caching;
using MasterDbLib.Lib.DataBaseServices;
using MasterDbLib.Lib.Messages;

namespace MasterDbLib.Lib
{
    public class MasterDB
    {
        public MasterDB(IDb db)
        {
            Db = db;
        }
        public static bool TurnOnEventualConsistencyReloading
        {
            set
            {
                AppDataBase.TurnOnEventualConsistencyReloading = value;

            }
            get
            {
                return AppDataBase.TurnOnEventualConsistencyReloading;
            }
        }

        /// <summary>
        /// Only works if set before any call is made to the database
        /// </summary>
        public static TimeSpan EventualConsistencyReloadingInterval
        {
            get
            {
                return AppDataBase.EventualConsistencyReloadingInterval;
            }
            set
            {
                 AppDataBase.EventualConsistencyReloadingInterval=value;
            }
        }
        private static IDb Db { set; get; }
        public T GetById<T>(string id) where T : IDbEntity, new()
        {
            CheckTypeIsserilizable<T>();
            return new DbActorFactory<T>(Db).GetById(id);
        }
        public async Task<bool> ReHydrateDataAsync<T>(string id) where T : IDbEntity, new()
        {
            CheckTypeIsserilizable<T>();
          return await new DbActorFactory<T>(Db).ReHydrateData().ConfigureAwait(false);
        }

        private static void CheckTypeIsserilizable<T>() where T : IDbEntity, new()
        {
            if (!typeof(T).IsSerializable)
            {
                // todo waiting for akka 1.4 to be out to fix this issue so that client can use [serializable attribut] 
                //todo typeof(T).IsSerializable
                //todo https://github.com/akkadotnet/akka.net/issues/3161
                //todo then i will uncomment below instead of using json serialization
                //todo The type 'SerializableAttribute' exists in both 'Akka, Version=1.3.1.0, Culture=neutral, PublicKeyToken=null' and 'netstandard, Version=2.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'

               // throw new ArgumentException("The type must be serializable.", "source");
            }
        }

        public IEnumerable<T> LoadAll<T>() where T : IDbEntity, new()
        {
            CheckTypeIsserilizable<T>();
            return new DbActorFactory<T>(Db).LoadAll();
        }

        public async Task<string> CreateNewAsync<T>(T data) where T : IDbEntity, new()
        {
            CheckTypeIsserilizable<T>();
            return await new DbActorFactory<T>(Db).CreateNew(data).ConfigureAwait(false);
        }

        public async Task<DbActorResponseMessage> UpdateAsync<T>(T d, List<string> transactionIds = null) where T : IDbEntity, new()
        {
            CheckTypeIsserilizable<T>();
            return await new DbActorFactory<T>(Db).Update(d, transactionIds).ConfigureAwait(false);
        }

        public async Task<DbActorResponseMessage> DeleteAsync<T>(T d) where T : IDbEntity, new()
        {
            CheckTypeIsserilizable<T>();
            return await new DbActorFactory<T>(Db).Delete(d).ConfigureAwait(false);
        }
    }
}