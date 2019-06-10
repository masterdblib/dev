namespace MasterDbLib.Lib.Caching
{
    public class DbData<T>
    {
        public DbData(string id, T data, string etag)
        {
            Id = id;
            Data = data;
            Etag = etag;
        }

        public string Id { private set; get; }
        public string Etag { private set; get; }
        public T Data { private set; get; }
    }
}