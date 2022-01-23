using System.Collections.Generic;

namespace SharedLib
{
    public readonly struct TalentTreeElement
    {
        public int TierID { get; init; }
        public int ColumnIndex { get; init; }
        public int TabID { get; init; }
        public List<int> SpellIds { get; init; }
    }
}
