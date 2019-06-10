using System.Collections.Generic;
using System.Threading.Tasks;
using MasterDbLib.Lib.Messages;

namespace MasterDbLib.Lib.DataBaseServices
{
    public interface IDb
   {
       Task<T> GetById<T>(string id) where T : IDbEntity, new();
      Task< IEnumerable<T>> LoadAll<T>() where T : IDbEntity, new();
       Task<string> CreateNew<T>(T data) where T : IDbEntity, new();
       Task<bool> Update<T>(T data) where T : IDbEntity, new();
       Task<bool> DeleteById<T>(string id) where T : IDbEntity, new();
   }
}
