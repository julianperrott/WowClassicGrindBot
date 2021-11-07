using System;

namespace Core
{
#pragma warning disable CA1815 // Override equals and operator equals on value types
    public struct Bag
#pragma warning restore CA1815 // Override equals and operator equals on value types
    {
        public int ItemId { get; set; }
        public BagType BagType { get; set; }
        public int SlotCount { get; set; }
        public int FreeSlot { get; set; }
    }
}
