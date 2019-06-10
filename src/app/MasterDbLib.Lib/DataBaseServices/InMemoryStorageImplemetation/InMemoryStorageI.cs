using MasterDbLib.Lib.Messages;
using MasterDbLib.Lib.Utility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MasterDbLib.Lib.DataBaseServices.InMemoryStorageImplemetation
{
    public class InMemoryStorage : IDb
    {
        private static ConcurrentDictionary<Type, ConcurrentDictionary<string, IDbEntity>> database = new ConcurrentDictionary<Type, ConcurrentDictionary<string, IDbEntity>>();

        T RemoveRef<T>(T d)
        {
          return  JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(d));
        }
        public InMemoryStorage()
        {
        }

        public Task<T> GetById<T>(string id) where T : IDbEntity, new()
        {
            SetUpData<T>();
            return Task.FromResult(RemoveRef((T)database[typeof(T)][id]));
        }

        public Task<IEnumerable<T>> LoadAll<T>() where T : IDbEntity, new()
        {
            SetUpData<T>();
            return Task.FromResult(database[typeof(T)].Select(x => RemoveRef((T)x.Value)));
        }

        private static void SetUpData<T>() where T : IDbEntity, new()
        {
            if (!database.ContainsKey(typeof(T)))
            {
                database.GetOrAdd(typeof(T), new ConcurrentDictionary<string, IDbEntity>());
            }
        }
        public Task<string> CreateNew<T>(T data) where T : IDbEntity, new()
        {
            SetUpData<T>();
            var id = StorageIdentityGenerator.GenerateId();
            data.Id = id;
            database[typeof(T)].GetOrAdd(id, RemoveRef(data));
            return Task.FromResult(id);
        }
        public Task<bool> Update<T>(T data) where T : IDbEntity, new()
        {
            SetUpData<T>();
            var existingData = database[typeof(T)][data.Id];

            if (existingData.Etag != data.Etag)
            {
                throw new AccessViolationException($"Etag mis-match between {existingData.Etag}  and {data.Etag}");
            }
            data.Etag = StorageIdentityGenerator.GenerateId();
            database[typeof(T)][data.Id] = RemoveRef(data);
            return Task.FromResult(true);
        }

        public Task<bool> DeleteById<T>(string id) where T : IDbEntity, new()
        {
            SetUpData<T>();
            var existingData = database[typeof(T)][id];

            return Task.FromResult(database[typeof(T)].TryRemove(id, out IDbEntity data));
        }
    }
}