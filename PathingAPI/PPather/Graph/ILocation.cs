namespace Common
{
    public interface ILocation
    {
        float X { get; }
        float Y { get; }
        float Z { get; }

        string ToPatherString();
    }
}