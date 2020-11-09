namespace Libs
{
    public interface IBlacklist
    {
        bool IsTargetBlacklisted();

        void Add(string name);
    }
}