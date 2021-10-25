using System.Collections.Generic;

namespace Core.Talents
{
#pragma warning disable CA1815 // Override equals and operator equals on value types
    public struct TalentTreeElement
#pragma warning restore CA1815 // Override equals and operator equals on value types
    {
        public int TierID { set; get; }
        public int ColumnIndex { set; get; }
        public int TabID { set; get; }
        public List<int> SpellIds { set; get; }
    }
}
