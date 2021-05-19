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

    public struct Bag : IEquatable<Bag>
    {
        public int ItemId { get; set; }
        public BagType BagType { get; set; }
        public long SlotCount { get; set; }
        public int FreeSlot { get; set; }

        public override bool Equals(object obj)
        {
            return false;
        }

        public override int GetHashCode()
        {
            return 0;
        }

        public static bool operator ==(Bag left, Bag right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Bag left, Bag right)
        {
            return !(left == right);
        }

        public bool Equals(Bag other)
        {
            throw new NotImplementedException();
        }
    }
}
