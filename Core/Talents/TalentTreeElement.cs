namespace Core.Talents
{
    public class TalentTreeElement
    {
        public int TierID { get; set; }
        public int ColumnIndex { get; set; }
        public int TabID { get; set; }
        public int[] SpellIds { get; set; } = new int[5];
    }
}
