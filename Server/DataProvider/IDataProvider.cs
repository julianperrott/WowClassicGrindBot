
namespace Server
{
    public interface IDataProvider
    {
        bool Enabled { get; set; }
        bool HasData();
        byte[] GetData();
    }
}
