namespace Core
{
#pragma warning disable CA1815 // Override equals and operator equals on value types
    public struct Spell
#pragma warning restore CA1815 // Override equals and operator equals on value types
    {
        public int Id { get; }
        public string Name { get; }
        public int Level { get; }
    }
}
