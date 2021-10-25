namespace Core.Talents
{
#pragma warning disable CA1815 // Override equals and operator equals on value types
    public struct TalentTab
#pragma warning restore CA1815 // Override equals and operator equals on value types
    {
        public int Id { get; }
        public string Name { get; }
        public string BackgroundFile { get; }
        public int OrderIndex { get; }
    }
}
