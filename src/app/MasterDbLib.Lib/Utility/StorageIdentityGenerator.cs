using System;
using System.Threading;

namespace MasterDbLib.Lib.Utility
{
    public class StorageIdentityGenerator
    {
        static long lastTimeStamp = DateTime.UtcNow.Ticks;

        static long UtcNowTicks
        {
            get
            {
                long original, newValue;
                do
                {
                    original = lastTimeStamp;
                    long now = DateTime.UtcNow.Ticks;
                    newValue = Math.Max(now, original + 1);
                }
                while (Interlocked.CompareExchange(ref lastTimeStamp, newValue, original) != original);

                //https://docs.microsoft.com/en-us/azure/cosmos-db/table-storage-design-guide#log-tail-pattern
                //RowKey that naturally sorts in reverse
                //date/time order by using so the most recent entry is always the first one in the table.
                return DateTime.MaxValue.Ticks - newValue;
                //todo canot use the tail log pattern coz varying part key will not allow one to do atomic operation
                //return  newValue;
            }
        }

        public static string GenerateId()
        {
            return UtcNowTicks.ToString();
        }
      
    }
}