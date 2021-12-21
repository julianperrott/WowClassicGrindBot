namespace Core
{
    public class NoBlacklist : IBlacklist
    {
        public void Add(string name)
        {
        }

        public bool IsTargetBlacklisted()
        {
            return false;
        }
    }
}