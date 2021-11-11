namespace Core.Talents
{
#pragma warning disable CA1815 // Override equals and operator equals on value types
    public struct Talent
#pragma warning restore CA1815 // Override equals and operator equals on value types
    {
        public int Hash { get; set; }
        public int TabNum { get; set; }
        public int TierNum { get; set; }
        public int ColumnNum { get; set; }
        public int CurrentRank { get; set; }
        public string Name { get; set; }

        public override string ToString()
        {
            return $"{TabNum} - {TierNum} - {ColumnNum} - {CurrentRank} - {Name}";
        }
    }
}
