namespace Core.Talents
{
    public class Talent
    {
        public int Hash { get; set; }
        public int TabNum { get; set; }
        public int TierNum { get; set; }
        public int ColumnNum { get; set; }
        public int CurrentRank { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
