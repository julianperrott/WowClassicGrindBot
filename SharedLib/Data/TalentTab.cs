namespace SharedLib
{
#pragma warning disable CA1815 // Override equals and operator equals on value types
    public struct TalentTab
#pragma warning restore CA1815 // Override equals and operator equals on value types
    {
        public int Id { set; get; }
        public string Name { set; get; }
        public string BackgroundFile { set; get; }
        public int OrderIndex { get; }
    }
}
