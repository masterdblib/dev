using System;
using System.IO;
using Newtonsoft.Json;

namespace MasterDbLib.Lib.Utility
{
    public class DbEntityHelper
    {
        //public string Id { set; get; }
        //public string Etag { set; get; }

        public static T Clone<T>(T source)
        {
            //if (!typeof(T).IsSerializable)
            //{
            //    throw new ArgumentException("The type must be serializable.", "source");
            //}

            if (Object.ReferenceEquals(source, null))
            {
                return default(T);
            }

            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(source));
            // todo waiting for akka 1.4 to be out to fix this issue so that client can use [serializable attribut] 
            //todo typeof(T).IsSerializable
            //todo https://github.com/akkadotnet/akka.net/issues/3161
            //todo then i will uncomment below instead of using json serialization
            //todo The type 'SerializableAttribute' exists in both 'Akka, Version=1.3.1.0, Culture=neutral, PublicKeyToken=null' and 'netstandard, Version=2.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
            System.Runtime.Serialization.IFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            Stream stream = new MemoryStream();
            using (stream)
            {
                formatter.Serialize(stream, source);
                stream.Seek(0, SeekOrigin.Begin);
                return (T)formatter.Deserialize(stream);
            }
        }
    }
}