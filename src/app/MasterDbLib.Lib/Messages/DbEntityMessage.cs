using MasterDbLib.Lib.DataBaseServices;

namespace MasterDbLib.Lib.Messages
{
    public class DbEntityMessage<T> where T : IDbEntity
    {
        public DbEntityMessage(DbCommand dbCommand, T entity)
        {
            DbCommand = dbCommand;
            Entity = entity;
        }

        public T Entity { get; private set; }
        public DbCommand DbCommand { get; private set; }
    }
}