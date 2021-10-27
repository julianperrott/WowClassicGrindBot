namespace SharedLib
{
#pragma warning disable CA1815 // Override equals and operator equals on value types
    public struct Spell
#pragma warning restore CA1815 // Override equals and operator equals on value types
    {
        public int Id { set; get; }
        public string Name { set; get; }
        public int Level { set; get; }

        public void SetLevel(int level)
        {
            Level = level;
        }
    }
}
