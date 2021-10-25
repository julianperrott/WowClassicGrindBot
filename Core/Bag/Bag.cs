using System;

namespace Core
{
    public enum BagType
    {
        Unspecified = 0,
        Quiver = 1,
        AmmoPouch = 2,
        SoulBag = 4,
        LeatherworkingBag = 8,
        InscriptionBag = 16,
        HerbBag = 32,
        EnchantingBag = 64,
        EngineeringBag = 128,
        Keyring = 256,
        GemBag = 512,
        MiningBag = 1024,
        Unknown = 2048,
        VanityPets = 4096
    }

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
