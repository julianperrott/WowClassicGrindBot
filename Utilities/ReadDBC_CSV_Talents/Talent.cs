namespace ReadDBC_CSV_Talents
{
    public class Talent
    {
        public int TierID { get; set; }
        public int ColumnIndex { get; set; }
        public int TabID { get; set; }
        public int[] SpellIds { get; set; }
    }
}
