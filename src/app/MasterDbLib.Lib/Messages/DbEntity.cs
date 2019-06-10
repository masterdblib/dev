namespace MasterDbLib.Lib.Messages
{
    public interface IDbEntity
    {
        string Id { set; get; }
        string Etag { set; get; }
    }
    
}