using System.Collections.Generic;

namespace Core.Talents
{
#pragma warning disable CA1815 // Override equals and operator equals on value types
    public struct TalentTreeElement
#pragma warning restore CA1815 // Override equals and operator equals on value types
    {
        public int TierID { get; }
        public int ColumnIndex { get; }
        public int TabID { get; }
        public List<int> SpellIds { get; }
    }
}
