using System.Collections.Generic;

namespace ReadDBC_CSV
{
    public struct TalentTreeElement
    {
        public int TierID { get; set; }
        public int ColumnIndex { get; set; }
        public int TabID { get; set; }
        public List<int> SpellIds { get; set; }
    }
}
